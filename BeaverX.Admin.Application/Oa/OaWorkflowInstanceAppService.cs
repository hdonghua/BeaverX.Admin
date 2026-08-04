using System.Text.Json;
using System.Security.Cryptography;
using BeaverX.Admin.Application.Contracts.Oa;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Oa;
using BeaverX.Admin.Domain.Shared.Oa;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories;

namespace BeaverX.Admin.Application.Oa;

public partial class OaWorkflowAppService
{
    public async Task LaunchAsync(OaLaunchRequest input, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var definition = await _definitions.GetAsync(input.FlowDefId, cancellationToken: cancellationToken);
        if (definition.Status != OaDefinitionStatus.Published) throw new BusinessException("流程未发布或已停用");
        await ValidateInitiatorAsync(definition, userId, cancellationToken);
        var launchForm = ParseFormObject(input.FlowValue);
        var launchFields = await GetFlowFormWidgetsAsync(definition.Id, cancellationToken);
        ValidateFormValues(launchForm, launchFields);

        var instance = new OaInstance(_ids.Create())
        {
            InstanceNo = await GenerateInstanceNoAsync(cancellationToken),
            DefId = definition.Id, Initiator = userId,
            FormValue = input.FlowValue, Status = OaInstanceStatus.Underway
        };
        await _instances.InsertAsync(instance, autoSave: true, cancellationToken: cancellationToken);
        await AddLogAsync(instance.Id, null, userId, OaOperationType.Start, null, null, null, cancellationToken);

        var allNodes = await (await _nodes.GetQueryableAsync()).Where(x => x.DefId == definition.Id).ToListAsync(cancellationToken);
        var start = allNodes.FirstOrDefault(x => x.NodeType == OaNodeType.Start) ?? allNodes.FirstOrDefault();
        if (start == null) throw new BusinessException("流程定义没有节点");
        await ContinueAsync(instance, start, allNodes, input.Designees, includeCurrent: true, cancellationToken);
    }

    public async Task<List<OaFlowChartNodeDto>> ViewProcessChartAsync(Guid defId, CancellationToken cancellationToken = default)
    {
        var nodes = await (await _nodes.GetQueryableAsync()).AsNoTracking().Where(x => x.DefId == defId).ToListAsync(cancellationToken);
        var nodeIds = nodes.Select(x => x.Id).ToList();
        var approvers = await (await _approvers.GetQueryableAsync()).AsNoTracking().Where(x => nodeIds.Contains(x.NodeId)).ToListAsync(cancellationToken);
        var ccs = await (await _ccConfigs.GetQueryableAsync()).AsNoTracking().Where(x => nodeIds.Contains(x.NodeId)).ToListAsync(cancellationToken);
        var transactors = await (await _transactConfigs.GetQueryableAsync()).AsNoTracking().Where(x => nodeIds.Contains(x.NodeId)).ToListAsync(cancellationToken);
        return nodes.Where(x => !x.IsConditionBranch).Select(node =>
        {
            var options = GetNodeRuntimeOptions(node);
            return new OaFlowChartNodeDto
            {
            Id = node.Id, NodeId = node.Id, Name = node.NodeName, NodeType = (int)node.NodeType,
            ApprovalType = node.ApprovalType, MultiInstanceApprovalType = node.MultiInstanceApprovalType ?? 0,
            FlowNodeNoAuditorType = node.FlowNodeNoAuditorType ?? 0,
            FlowNodeNoAuditorAssignee = node.FlowNodeNoAuditorAssignee,
            FlowNodeAuditAdmin = options.FlowNodeAuditAdmin,
            UserIds = node.NodeType switch
            {
                OaNodeType.Copy => ccs.Where(x => x.NodeId == node.Id && x.CcType != (int)OaAssigneeType.Role).SelectMany(x => x.Assignees).Distinct().ToList(),
                OaNodeType.Transact => transactors.Where(x => x.NodeId == node.Id && x.AssigneeType != (int)OaAssigneeType.Role).SelectMany(x => x.Assignees).Distinct().ToList(),
                _ => approvers.Where(x => x.NodeId == node.Id && x.AssigneeType != OaAssigneeType.Role).SelectMany(x => x.Assignees).Distinct().ToList()
            },
            RoleIds = node.NodeType switch
            {
                OaNodeType.Copy => ccs.Where(x => x.NodeId == node.Id && x.CcType == (int)OaAssigneeType.Role).SelectMany(x => x.Roles.Count > 0 ? x.Roles : x.Assignees).Distinct().ToList(),
                OaNodeType.Transact => transactors.Where(x => x.NodeId == node.Id && x.AssigneeType == (int)OaAssigneeType.Role).SelectMany(x => x.Roles.Count > 0 ? x.Roles : x.Assignees).Distinct().ToList(),
                _ => approvers.Where(x => x.NodeId == node.Id && x.AssigneeType == OaAssigneeType.Role).SelectMany(x => x.Roles.Count > 0 ? x.Roles : x.Assignees).Distinct().ToList()
            },
            InitatorChoice = node.NodeType == OaNodeType.Transact
                ? transactors.Any(x => x.NodeId == node.Id && x.AssigneeType == (int)OaAssigneeType.InitiatorChoice)
                : approvers.Any(x => x.NodeId == node.Id && x.AssigneeType == OaAssigneeType.InitiatorChoice)
            };
        }).ToList();
    }

    public Task<PagedResultDto<OaFlowInstanceListDto>> QueryPendingAsync(OaFlowInstanceQuery input, CancellationToken cancellationToken = default) =>
        QueryInstancePageAsync(input, QueryScope.Pending, cancellationToken);
    public Task<PagedResultDto<OaFlowInstanceListDto>> QueryMyApplyAsync(OaFlowInstanceQuery input, CancellationToken cancellationToken = default) =>
        QueryInstancePageAsync(input, QueryScope.Mine, cancellationToken);
    public Task<PagedResultDto<OaFlowInstanceListDto>> QueryCcAsync(OaFlowInstanceQuery input, CancellationToken cancellationToken = default) =>
        QueryInstancePageAsync(input, QueryScope.Cc, cancellationToken);
    public Task<PagedResultDto<OaFlowInstanceListDto>> QueryAuditedAsync(OaFlowInstanceQuery input, CancellationToken cancellationToken = default) =>
        QueryInstancePageAsync(input, QueryScope.Audited, cancellationToken);

    public async Task<OaFlowInstanceDetailsDto> GetDetailsAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        var instance = await _instances.GetAsync(instanceId, cancellationToken: cancellationToken);
        var fields = await GetFlowFormWidgetsAsync(instance.DefId, cancellationToken);
        var nodes = await (await _nodes.GetQueryableAsync()).AsNoTracking().Where(x => x.DefId == instance.DefId).ToListAsync(cancellationToken);
        var tasks = await (await _tasks.GetQueryableAsync()).AsNoTracking().Where(x => x.InstanceId == instanceId).OrderBy(x => x.CreationTime).ToListAsync(cancellationToken);
        if (_currentUser.Id is { } currentUserId)
        {
            var activeTask = tasks.FirstOrDefault(x => x.UserId == currentUserId && x.Status == OaTaskStatus.Pending);
            var activeNode = activeTask == null ? null : nodes.FirstOrDefault(x => x.Id == activeTask.NodeId);
            if (activeNode != null) ApplyNodeFormAuth(fields, activeNode);
        }

        var taskNodes = tasks.Select(task =>
        {
            var node = nodes.First(x => x.Id == task.NodeId);
            return new OaFlowInstanceNodeDto
            {
                Id = task.Id, Name = task.NodeName, FlowInstId = instanceId, FlowNodeId = task.NodeId,
                FlowNodeName = task.NodeName, UserIds = [task.UserId.ToString()], Underway = task.Status == OaTaskStatus.Pending,
                Type = (int)node.NodeType, NodeType = (int)node.NodeType, MultiInstanceApprovalType = node.MultiInstanceApprovalType ?? 0,
                FlowCmd = task.FlowCmd.HasValue ? (int)task.FlowCmd.Value : null, AuditTime = task.CompleteTime,
                Auditor = task.UserId.ToString(), Assignee = task.UserId.ToString(), Comment = task.Remark
            };
        }).ToList();
        var touched = tasks.Select(x => x.NodeId).ToHashSet();
        var future = nodes.Where(x => !x.IsConditionBranch && !touched.Contains(x.Id) && x.NodeType != OaNodeType.Start)
            .Select(x => new OaFlowInstanceNodeDto
            {
                Id = x.Id, Name = x.NodeName, FlowInstId = instanceId, FlowNodeId = x.Id, FlowNodeName = x.NodeName,
                Type = (int)x.NodeType, NodeType = (int)x.NodeType, MultiInstanceApprovalType = x.MultiInstanceApprovalType ?? 0
            }).ToList();
        return new OaFlowInstanceDetailsDto { FormValue = instance.FormValue, FormWidgets = fields, Nodes = taskNodes, FutureNodes = future };
    }

    public async Task<OaFlowInstanceListDto> GetInstanceSummaryAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        var instance = await _instances.GetAsync(instanceId, cancellationToken: cancellationToken);
        var definition = await _definitions.GetAsync(instance.DefId, cancellationToken: cancellationToken);
        var task = await (await _tasks.GetQueryableAsync()).AsNoTracking()
            .Where(x => x.InstanceId == instanceId).OrderByDescending(x => x.Status == OaTaskStatus.Pending)
            .ThenByDescending(x => x.CreationTime).FirstOrDefaultAsync(cancellationToken);
        var node = task == null ? null : await _nodes.FindAsync(task.NodeId, cancellationToken: cancellationToken);
        var summaryFields = await (await _fields.GetQueryableAsync()).AsNoTracking()
            .Where(x => x.DefId == instance.DefId && x.IsSummary).OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return new OaFlowInstanceListDto
        {
            FlowDefId = definition.Id, Name = definition.Name, GroupId = definition.GroupId, Cancelable = definition.Cancelable,
            Id = instance.Id, InstanceNo = instance.InstanceNo, InitiatorId = instance.Initiator.ToString(), BeginTime = instance.CreationTime, EndTime = instance.EndTime,
            Status = (int)instance.Status, TaskId = task?.Id, ActNodeId = task?.NodeId,
            Assignable = node?.Assignable ?? false, Signable = node?.Signable ?? false, Backable = node?.Backable ?? false,
            Signature = node?.Signature ?? false, NodeType = node == null ? 0 : (int)node.NodeType,
            Summary = BuildSummary(instance.FormValue, summaryFields)
        };
    }

    private async Task<string> GenerateInstanceNoAsync(CancellationToken cancellationToken)
    {
        var date = DateTime.UtcNow.AddHours(8).ToString("yyyyMMdd");
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var instanceNo = $"{date}-{RandomNumberGenerator.GetHexString(4)}";
            if (!await _instances.AnyAsync(x => x.InstanceNo == instanceNo, cancellationToken)) return instanceNo;
        }

        throw new BusinessException("生成流程编号失败，请重试");
    }

    public async Task FormModifyAsync(OaFormModifyRequest input, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var task = await GetPendingTaskAsync(input.TaskId, userId, cancellationToken);
        if (task.InstanceId != input.FlowInstId || task.NodeId != input.FlowNodeId) throw new BusinessException("流程任务信息不匹配");
        var instance = await _instances.GetAsync(input.FlowInstId, cancellationToken: cancellationToken);
        var node = await _nodes.GetAsync(task.NodeId, cancellationToken: cancellationToken);
        EnsureOnlyEditableValuesChanged(instance.FormValue, input.FlowValue, node);
        var form = ParseFormObject(input.FlowValue);
        var fields = await GetFlowFormWidgetsAsync(instance.DefId, cancellationToken);
        ValidateFormValues(form, fields);
        if (instance.FormValue == input.FlowValue) return;
        var previousFormValue = instance.FormValue;
        instance.FormValue = input.FlowValue;
        await _instances.UpdateAsync(instance, autoSave: true, cancellationToken: cancellationToken);
        await AddLogAsync(instance.Id, task.Id, userId, OaOperationType.FormModified, task.NodeId, task.NodeId, previousFormValue, cancellationToken);
    }

    public Task<bool> HasFormEditRecordAsync(Guid instanceId, CancellationToken cancellationToken = default) =>
        _logs.AnyAsync(x => x.InstanceId == instanceId && x.OperationType == OaOperationType.FormModified, cancellationToken);

    public async Task<List<OaFormEditRecordDto>> GetFormEditRecordsAsync(Guid instanceId, CancellationToken cancellationToken = default) =>
        await (await _logs.GetQueryableAsync()).AsNoTracking()
            .Where(x => x.InstanceId == instanceId && x.OperationType == OaOperationType.FormModified)
            .OrderByDescending(x => x.CreationTime)
            .Select(x => new OaFormEditRecordDto
            {
                CreatorId = x.Operator.ToString(), CreateTime = x.CreationTime, FormValue = x.Remark ?? "{}"
            }).ToListAsync(cancellationToken);

    public async Task UrgeAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var instance = await _instances.GetAsync(instanceId, cancellationToken: cancellationToken);
        if (instance.Status != OaInstanceStatus.Underway) throw new BusinessException("只能催办进行中的流程");
        if (!await _tasks.AnyAsync(x => x.InstanceId == instanceId && x.Status == OaTaskStatus.Pending, cancellationToken))
            throw new BusinessException("当前流程没有待办任务");
        await AddLogAsync(instanceId, null, userId, OaOperationType.Urge, null, null, "流程催办", cancellationToken);
    }

    public async Task CommentAsync(OaCommentRequest input, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(input.Content)) throw new BusinessException("评论内容不能为空");
        if (!await _instances.AnyAsync(x => x.Id == input.InstanceId, cancellationToken)) throw new BusinessException("流程实例不存在");
        await _comments.InsertAsync(new OaComment(_ids.Create())
        {
            InstanceId = input.InstanceId, TaskId = input.TaskId, Commenter = userId,
            Content = input.Content.Trim(), Attachment = input.Attachment
        }, autoSave: true, cancellationToken: cancellationToken);
        await AddLogAsync(input.InstanceId, input.TaskId, userId, OaOperationType.Comment, null, null, input.Content, cancellationToken);
    }

    public async Task ApproveAsync(OaTaskActionRequest input, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var task = await GetPendingTaskAsync(input.TaskId, userId, cancellationToken);
        var instance = await _instances.GetAsync(task.InstanceId, cancellationToken: cancellationToken);
        if (input.FlowCmd == (int)OaOperationType.Rejected)
        {
            task.Status = OaTaskStatus.Rejected;
            task.FlowCmd = OaOperationType.Rejected;
            task.CompleteTime = DateTime.UtcNow;
            task.Remark = input.Comment;
            instance.Status = OaInstanceStatus.Rejected;
            instance.EndTime = DateTime.UtcNow;
            await RecallPendingTasksAsync(instance.Id, task.Id, cancellationToken);
            await _tasks.UpdateAsync(task, autoSave: true, cancellationToken: cancellationToken);
            await _instances.UpdateAsync(instance, autoSave: true, cancellationToken: cancellationToken);
            await AddLogAsync(instance.Id, task.Id, userId, OaOperationType.Rejected, task.NodeId, null, input.Comment, cancellationToken);
            return;
        }
        if (input.FlowCmd != (int)OaOperationType.Approved && input.FlowCmd != (int)OaOperationType.Transact)
            throw new BusinessException("不支持的审批操作");

        task.Status = OaTaskStatus.Approved;
        task.FlowCmd = input.FlowCmd == (int)OaOperationType.Transact ? OaOperationType.Transact : OaOperationType.Approved;
        task.CompleteTime = DateTime.UtcNow;
        task.Remark = input.Comment;
        await _tasks.UpdateAsync(task, autoSave: true, cancellationToken: cancellationToken);
        await AddLogAsync(instance.Id, task.Id, userId, task.FlowCmd.Value, task.NodeId, null, input.Comment, cancellationToken);

        var node = await _nodes.GetAsync(task.NodeId, cancellationToken: cancellationToken);
        if (node.MultiInstanceApprovalType == 3 && task.CandidateUsers is { Count: > 0 })
        {
            var nextUser = task.CandidateUsers.FirstOrDefault(IsGuid);
            if (nextUser != null)
            {
                var remainingCandidates = task.CandidateUsers.SkipWhile(x => x != nextUser).Skip(1).ToList();
                await _tasks.InsertAsync(new OaTask(_ids.Create())
                {
                    InstanceId = instance.Id, NodeId = node.Id, NodeName = node.NodeName,
                    UserId = Guid.Parse(nextUser), Status = OaTaskStatus.Pending,
                    ParentTaskId = task.Id, CandidateUsers = remainingCandidates,
                    LoopCounter = (task.LoopCounter ?? 0) + 1
                }, autoSave: true, cancellationToken: cancellationToken);
                return;
            }
        }
        var remaining = await (await _tasks.GetQueryableAsync()).Where(x => x.InstanceId == instance.Id && x.NodeId == node.Id && x.Status == OaTaskStatus.Pending).ToListAsync(cancellationToken);
        if (node.MultiInstanceApprovalType == 2 && remaining.Count > 0)
        {
            foreach (var sibling in remaining) { sibling.Status = OaTaskStatus.Recalled; sibling.CompleteTime = DateTime.UtcNow; }
            await _tasks.UpdateManyAsync(remaining, autoSave: true, cancellationToken: cancellationToken);
            remaining.Clear();
        }
        if (remaining.Count > 0) return;

        var allNodes = await (await _nodes.GetQueryableAsync()).Where(x => x.DefId == instance.DefId).ToListAsync(cancellationToken);
        var next = node.ChildNodeId.HasValue ? allNodes.FirstOrDefault(x => x.Id == node.ChildNodeId.Value) : null;
        if (next == null) { await CompleteInstanceAsync(instance, cancellationToken); return; }
        await ContinueAsync(instance, next, allNodes, null, includeCurrent: true, cancellationToken);
    }

    public async Task AssignAsync(OaTaskActionRequest input, CancellationToken cancellationToken = default)
    {
        var operatorId = GetCurrentUserId();
        var task = await GetPendingTaskAsync(input.TaskId, operatorId, cancellationToken);
        var assignee = ParseUserId(input.Assignee, "请选择转交人");
        if (assignee == operatorId) throw new BusinessException("不能转交给自己");
        task.Status = OaTaskStatus.Transferred;
        task.FlowCmd = OaOperationType.Assign;
        task.CompleteTime = DateTime.UtcNow;
        task.Remark = input.Comment;
        await _tasks.UpdateAsync(task, autoSave: true, cancellationToken: cancellationToken);
        await _tasks.InsertAsync(new OaTask(_ids.Create())
        {
            InstanceId = task.InstanceId, NodeId = task.NodeId, NodeName = task.NodeName,
            UserId = assignee, Status = OaTaskStatus.Pending, ParentTaskId = task.Id
        }, autoSave: true, cancellationToken: cancellationToken);
        await AddLogAsync(task.InstanceId, task.Id, operatorId, OaOperationType.Assign, task.NodeId, task.NodeId, input.Comment, cancellationToken);
    }

    public async Task AddSignAsync(OaTaskActionRequest input, CancellationToken cancellationToken = default)
    {
        var operatorId = GetCurrentUserId();
        var task = await GetPendingTaskAsync(input.TaskId, operatorId, cancellationToken);
        var userId = ParseUserId(input.UserId ?? input.Assignee, "请选择加签人员");
        if (await _tasks.AnyAsync(x => x.InstanceId == task.InstanceId && x.NodeId == task.NodeId && x.UserId == userId && x.Status == OaTaskStatus.Pending, cancellationToken))
            throw new BusinessException("该人员已在当前审批节点中");
        await _tasks.InsertAsync(new OaTask(_ids.Create())
        {
            InstanceId = task.InstanceId, NodeId = task.NodeId, NodeName = task.NodeName,
            UserId = userId, Status = OaTaskStatus.Pending, ParentTaskId = task.Id, FlowCmd = OaOperationType.AddSign
        }, autoSave: true, cancellationToken: cancellationToken);
        await AddLogAsync(task.InstanceId, task.Id, operatorId, OaOperationType.AddSign, task.NodeId, task.NodeId, input.Comment, cancellationToken);
    }

    public async Task DelSignAsync(OaTaskActionRequest input, CancellationToken cancellationToken = default)
    {
        var operatorId = GetCurrentUserId();
        var task = await GetPendingTaskAsync(input.TaskId, operatorId, cancellationToken);
        var targetId = string.IsNullOrWhiteSpace(input.UserId) ? (Guid?)null : ParseUserId(input.UserId, "减签人员无效");
        var candidates = await (await _tasks.GetQueryableAsync()).Where(x => x.InstanceId == task.InstanceId && x.NodeId == task.NodeId && x.Status == OaTaskStatus.Pending && x.Id != task.Id).ToListAsync(cancellationToken);
        var target = targetId.HasValue ? candidates.FirstOrDefault(x => x.UserId == targetId.Value) : candidates.FirstOrDefault(x => x.ParentTaskId.HasValue);
        if (target == null) throw new BusinessException("没有可减签的人员");
        target.Status = OaTaskStatus.Recalled;
        target.FlowCmd = OaOperationType.DelSign;
        target.CompleteTime = DateTime.UtcNow;
        await _tasks.UpdateAsync(target, autoSave: true, cancellationToken: cancellationToken);
        await AddLogAsync(task.InstanceId, task.Id, operatorId, OaOperationType.DelSign, task.NodeId, task.NodeId, input.Comment, cancellationToken);
    }

    public async Task JumpAsync(OaTaskActionRequest input, CancellationToken cancellationToken = default)
    {
        var operatorId = GetCurrentUserId();
        var task = await GetPendingTaskAsync(input.TaskId, operatorId, cancellationToken);
        var node = await _nodes.GetAsync(task.NodeId, cancellationToken: cancellationToken);
        if (!node.Backable) throw new BusinessException("当前节点不允许回退");
        var allNodes = await (await _nodes.GetQueryableAsync()).Where(x => x.DefId == node.DefId).ToListAsync(cancellationToken);
        var target = input.TargetNodeId.HasValue ? allNodes.FirstOrDefault(x => x.Id == input.TargetNodeId.Value) : allNodes.FirstOrDefault(x => x.ChildNodeId == node.Id && x.NodeType == OaNodeType.Approve);
        if (target == null) throw new BusinessException("没有可回退的节点");
        task.Status = OaTaskStatus.Approved;
        task.FlowCmd = OaOperationType.Back;
        task.CompleteTime = DateTime.UtcNow;
        await _tasks.UpdateAsync(task, autoSave: true, cancellationToken: cancellationToken);
        var instance = await _instances.GetAsync(task.InstanceId, cancellationToken: cancellationToken);
        await ContinueAsync(instance, target, allNodes, null, includeCurrent: true, cancellationToken);
        await AddLogAsync(instance.Id, task.Id, operatorId, OaOperationType.Back, node.Id, target.Id, input.Comment, cancellationToken);
    }

    public async Task CancelAsync(OaTaskActionRequest input, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var instance = await _instances.GetAsync(input.FlowInstId, cancellationToken: cancellationToken);
        var definition = await _definitions.GetAsync(instance.DefId, cancellationToken: cancellationToken);
        if (instance.Initiator != userId) throw new BusinessException("只能撤销自己发起的流程");
        if (!definition.Cancelable || instance.Status != OaInstanceStatus.Underway) throw new BusinessException("当前流程不可撤销");
        instance.Status = OaInstanceStatus.Cancelled;
        instance.EndTime = DateTime.UtcNow;
        await RecallPendingTasksAsync(instance.Id, null, cancellationToken);
        await _instances.UpdateAsync(instance, autoSave: true, cancellationToken: cancellationToken);
        await AddLogAsync(instance.Id, input.TaskId == Guid.Empty ? null : input.TaskId, userId, OaOperationType.Canceled, null, null, input.Comment, cancellationToken);
    }

    private async Task ContinueAsync(OaInstance instance, OaNode current, List<OaNode> allNodes, Dictionary<Guid, List<string>>? designees, bool includeCurrent, CancellationToken cancellationToken)
    {
        var node = includeCurrent ? current : current.ChildNodeId.HasValue ? allNodes.FirstOrDefault(x => x.Id == current.ChildNodeId.Value) : null;
        for (var guard = 0; guard < allNodes.Count + 5 && node != null; guard++)
        {
            if (node.NodeType == OaNodeType.End) { await CompleteInstanceAsync(instance, cancellationToken); return; }
            if (node.NodeType == OaNodeType.ExclusiveGateway)
            {
                node = await SelectConditionBranchAsync(instance, node, allNodes, cancellationToken);
                continue;
            }
            if (node.NodeType == OaNodeType.Condition || node.NodeType == OaNodeType.Start || node.NodeType == OaNodeType.Trigger)
            {
                node = node.ChildNodeId.HasValue ? allNodes.FirstOrDefault(x => x.Id == node.ChildNodeId.Value) : null;
                continue;
            }
            if (node.NodeType == OaNodeType.Copy)
            {
                await CreateCcRecordsAsync(instance, node, cancellationToken);
                node = node.ChildNodeId.HasValue ? allNodes.FirstOrDefault(x => x.Id == node.ChildNodeId.Value) : null;
                continue;
            }
            if ((node.NodeType == OaNodeType.Approve || node.NodeType == OaNodeType.Transact) && node.ApprovalType == 1)
            {
                await AddLogAsync(instance.Id, null, instance.Initiator, OaOperationType.AutoApproved, node.Id, node.ChildNodeId, null, cancellationToken);
                node = node.ChildNodeId.HasValue ? allNodes.FirstOrDefault(x => x.Id == node.ChildNodeId.Value) : null;
                continue;
            }
            if (node.NodeType == OaNodeType.Approve && node.ApprovalType == 2)
            {
                instance.Status = OaInstanceStatus.Rejected;
                instance.EndTime = DateTime.UtcNow;
                await _instances.UpdateAsync(instance, autoSave: true, cancellationToken: cancellationToken);
                await AddLogAsync(instance.Id, null, instance.Initiator, OaOperationType.AutoRejected, node.Id, null, null, cancellationToken);
                return;
            }
            if (node.NodeType == OaNodeType.Approve || node.NodeType == OaNodeType.Transact)
            {
                var users = await ResolveAssigneesAsync(instance, node, designees, cancellationToken);
                var skippedSelf = false;
                if (users.Contains(instance.Initiator))
                {
                    if (node.FlowNodeSelfAuditorType == 1)
                    {
                        users.RemoveAll(x => x == instance.Initiator);
                        skippedSelf = users.Count == 0;
                    }
                    else if (node.FlowNodeSelfAuditorType is 2 or 3)
                    {
                        users.RemoveAll(x => x == instance.Initiator);
                        var replacements = node.FlowNodeSelfAuditorType == 2
                            ? await ResolveManagerChainAsync(instance.Initiator, 0, 0, false, cancellationToken)
                            : await ResolveDepartmentLeaderChainAsync(instance.Initiator, 1, 0, true, cancellationToken);
                        var replacement = replacements.FirstOrDefault(x => x != instance.Initiator);
                        if (replacement != Guid.Empty) users.Add(replacement);
                    }
                }
                users = users.Distinct().ToList();
                if (skippedSelf)
                {
                    await AddLogAsync(instance.Id, null, instance.Initiator, OaOperationType.AutoApproved, node.Id, node.ChildNodeId, "发起人与审批人相同，自动跳过", cancellationToken);
                    node = node.ChildNodeId.HasValue ? allNodes.FirstOrDefault(x => x.Id == node.ChildNodeId.Value) : null;
                    continue;
                }
                if (users.Count == 0)
                {
                    if (node.FlowNodeNoAuditorType == 1 && Guid.TryParse(node.FlowNodeNoAuditorAssignee, out var fallback)) users.Add(fallback);
                    else if (node.FlowNodeNoAuditorType == 2)
                    {
                        var options = GetNodeRuntimeOptions(node);
                        if (Guid.TryParse(options.FlowNodeAuditAdmin, out var admin)) users.Add(admin);
                        else
                        {
                            var definition = await _definitions.GetAsync(instance.DefId, cancellationToken: cancellationToken);
                            var defaultAdmin = definition.FlowAdminIds.FirstOrDefault(IsGuid);
                            if (defaultAdmin != null) users.Add(Guid.Parse(defaultAdmin));
                        }
                    }
                    else
                    {
                        await AddLogAsync(instance.Id, null, instance.Initiator, OaOperationType.AutoApproved, node.Id, node.ChildNodeId, "审批人为空，自动通过", cancellationToken);
                        node = node.ChildNodeId.HasValue ? allNodes.FirstOrDefault(x => x.Id == node.ChildNodeId.Value) : null;
                        continue;
                    }
                }
                if (users.Count == 0)
                {
                    throw new BusinessException($"节点“{node.NodeName}”没有可用的审批人或流程管理员");
                }
                var distinctUsers = users.Distinct().ToList();
                var taskUsers = node.MultiInstanceApprovalType == 3 ? distinctUsers.Take(1) : distinctUsers;
                var candidates = node.MultiInstanceApprovalType == 3 ? distinctUsers.Skip(1).Select(x => x.ToString()).ToList() : null;
                var tasks = taskUsers.Select(userId => new OaTask(_ids.Create())
                    {
                        InstanceId = instance.Id, NodeId = node.Id, NodeName = node.NodeName,
                        UserId = userId, Status = OaTaskStatus.Pending, CandidateUsers = candidates,
                        LoopCounter = node.MultiInstanceApprovalType == 3 ? 0 : null
                    }).ToList();
                await _tasks.InsertManyAsync(tasks, autoSave: true, cancellationToken: cancellationToken);
                return;
            }
            node = node.ChildNodeId.HasValue ? allNodes.FirstOrDefault(x => x.Id == node.ChildNodeId.Value) : null;
        }
        if (node == null) await CompleteInstanceAsync(instance, cancellationToken);
    }

    private async Task<List<Guid>> ResolveAssigneesAsync(OaInstance instance, OaNode node, Dictionary<Guid, List<string>>? designees, CancellationToken cancellationToken)
    {
        var configs = node.NodeType == OaNodeType.Transact
            ? await (await _transactConfigs.GetQueryableAsync()).AsNoTracking().Where(x => x.NodeId == node.Id)
                .Select(x => new AssigneeConfig((OaAssigneeType)x.AssigneeType, x.Assignees, x.Roles, x.LayerType, x.Layer)).ToListAsync(cancellationToken)
            : await (await _approvers.GetQueryableAsync()).AsNoTracking().Where(x => x.NodeId == node.Id)
                .Select(x => new AssigneeConfig(x.AssigneeType, x.Assignees, x.Roles, x.LayerType, x.Layer)).ToListAsync(cancellationToken);
        var result = new List<Guid>();
        foreach (var config in configs)
        {
            if (config.AssigneeType == OaAssigneeType.InitiatorChoice)
            {
                if (designees?.TryGetValue(node.Id, out var selected) == true)
                    result.AddRange(selected.Where(IsGuid).Select(Guid.Parse));
            }
            else
                result.AddRange(await ResolveConfiguredUsersAsync(instance.Initiator, config.AssigneeType, config.Assignees, config.Roles, config.LayerType, config.Layer, cancellationToken));
        }
        return result.Distinct().ToList();
    }

    private sealed record AssigneeConfig(OaAssigneeType AssigneeType, List<string> Assignees, List<string> Roles, int? LayerType, int? Layer);

    private async Task CreateCcRecordsAsync(OaInstance instance, OaNode node, CancellationToken cancellationToken)
    {
        var configs = await (await _ccConfigs.GetQueryableAsync()).AsNoTracking().Where(x => x.NodeId == node.Id).ToListAsync(cancellationToken);
        var users = new List<Guid>();
        foreach (var config in configs)
        {
            var ccType = (OaAssigneeType)config.CcType;
            users.AddRange(await ResolveConfiguredUsersAsync(instance.Initiator, ccType, config.Assignees, config.Roles, config.LayerType, config.Layer, cancellationToken));
        }
        var existing = await (await _ccRecords.GetQueryableAsync()).AsNoTracking().Where(x => x.InstanceId == instance.Id).Select(x => x.UserId).ToListAsync(cancellationToken);
        var records = users.Distinct().Except(existing).Select(userId => new OaCcRecord(_ids.Create()) { InstanceId = instance.Id, NodeId = node.Id, UserId = userId }).ToList();
        if (records.Count > 0) await _ccRecords.InsertManyAsync(records, autoSave: true, cancellationToken: cancellationToken);
    }

    private async Task<OaNode?> SelectConditionBranchAsync(OaInstance instance, OaNode gateway, List<OaNode> allNodes, CancellationToken cancellationToken)
    {
        var branches = allNodes.Where(x => x.ParentNodeId == gateway.Id && x.IsConditionBranch).OrderBy(x => x.PriorityLevel).ToList();
        foreach (var branch in branches)
        {
            if (await MatchesBranchAsync(instance, branch, cancellationToken))
                return branch.ChildNodeId.HasValue ? allNodes.FirstOrDefault(x => x.Id == branch.ChildNodeId.Value) : null;
        }
        var fallback = branches.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.ConditionExpression));
        return fallback?.ChildNodeId is Guid childId ? allNodes.FirstOrDefault(x => x.Id == childId) : null;
    }

    private async Task<bool> MatchesBranchAsync(OaInstance instance, OaNode branch, CancellationToken cancellationToken)
    {
        var groups = await (await _conditionGroups.GetQueryableAsync()).AsNoTracking().Where(x => x.NodeId == branch.Id).ToListAsync(cancellationToken);
        if (groups.Count == 0) return false;
        var groupIds = groups.Select(x => x.Id).ToList();
        var conditions = await (await _conditions.GetQueryableAsync()).AsNoTracking().Where(x => groupIds.Contains(x.GroupId)).ToListAsync(cancellationToken);
        var initiatorIdentities = await GetInitiatorIdentitiesAsync(instance.Initiator, cancellationToken);
        using var document = JsonDocument.Parse(instance.FormValue);
        return groups.Any(group => conditions.Where(x => x.GroupId == group.Id).All(condition => MatchCondition(initiatorIdentities, document.RootElement, condition)));
    }

    private static bool MatchCondition(HashSet<string> initiatorIdentities, JsonElement form, OaCondition condition)
    {
        var expected = condition.Values ?? [];
        if (condition.VarName == "initiator")
        {
            var contains = expected.Any(initiatorIdentities.Contains);
            return condition.Operator == 20 ? contains : !contains;
        }
        var actualValues = GetConditionValues(form, condition.VarName);
        if (actualValues.Count == 0) return false;
        var actualText = actualValues.FirstOrDefault() ?? string.Empty;
        var expectedText = expected.FirstOrDefault() ?? string.Empty;
        if (condition.Operator is >= 0 and <= 5 && decimal.TryParse(expectedText, out var right))
            return actualValues.Where(x => decimal.TryParse(x, out _)).Select(decimal.Parse).Any(left => condition.Operator switch
            {
                0 => left == right, 1 => left != right, 2 => left < right, 3 => left <= right,
                4 => left > right, 5 => left >= right, _ => false
            });
        return condition.Operator switch
        {
            10 => actualValues.Any(x => expectedText.Contains(x, StringComparison.OrdinalIgnoreCase)),
            11 => actualValues.All(x => !expectedText.Contains(x, StringComparison.OrdinalIgnoreCase)),
            12 => actualValues.Any(x => string.Equals(x, expectedText, StringComparison.OrdinalIgnoreCase)),
            13 => actualValues.All(x => !string.Equals(x, expectedText, StringComparison.OrdinalIgnoreCase)),
            14 => actualValues.Any(x => expected.Any(y => x.Contains(y, StringComparison.OrdinalIgnoreCase))),
            15 => actualValues.All(x => expected.All(y => !x.Contains(y, StringComparison.OrdinalIgnoreCase))),
            20 => actualValues.Intersect(expected, StringComparer.OrdinalIgnoreCase).Any(),
            21 => !actualValues.Intersect(expected, StringComparer.OrdinalIgnoreCase).Any(),
            _ => false
        };
    }

    private static List<string> GetConditionValues(JsonElement root, string propertyName)
    {
        var values = new List<string>();
        CollectConditionValues(root, propertyName, values);
        return values.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void CollectConditionValues(JsonElement element, string propertyName, List<string> values)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals(propertyName)) FlattenConditionValue(property.Value, values);
                else CollectConditionValues(property.Value, propertyName, values);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray()) CollectConditionValues(item, propertyName, values);
        }
    }

    private static void FlattenConditionValue(JsonElement element, List<string> values)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray()) FlattenConditionValue(item, values);
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("id", out var id)) values.Add(JsonValue(id));
            else foreach (var property in element.EnumerateObject()) FlattenConditionValue(property.Value, values);
        }
        else if (element.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            values.Add(JsonValue(element));
        }
    }

    private async Task ValidateInitiatorAsync(OaProcessDefinition definition, Guid userId, CancellationToken cancellationToken)
    {
        if (definition.PermissionType == OaPermissionType.None) throw new BusinessException("该流程不允许发起");
        if (definition.PermissionType == OaPermissionType.All) return;
        var rules = await (await _initiators.GetQueryableAsync()).AsNoTracking().Where(x => x.DefId == definition.Id).ToListAsync(cancellationToken);
        if (rules.Any(x => x.InitiatorType == 2 && x.InitiatorIds.Contains(userId.ToString()))) return;
        var roleIds = await (await _userRoles.GetQueryableAsync()).AsNoTracking().Where(x => x.UserId == userId).Select(x => x.RoleId.ToString()).ToListAsync(cancellationToken);
        if (rules.Any(x => x.InitiatorType == 1 && x.InitiatorIds.Intersect(roleIds).Any())) return;
        var deptIds = await (await _userDepartments.GetQueryableAsync()).AsNoTracking().Where(x => x.UserId == userId).Select(x => x.DepartmentId.ToString()).ToListAsync(cancellationToken);
        if (rules.Any(x => x.InitiatorType == 0 && x.InitiatorIds.Intersect(deptIds).Any())) return;
        throw new BusinessException("当前用户不在流程发起范围内");
    }

    private async Task<OaTask> GetPendingTaskAsync(Guid taskId, Guid userId, CancellationToken cancellationToken)
    {
        var task = await _tasks.FindAsync(taskId, cancellationToken: cancellationToken) ?? throw new BusinessException("审批任务不存在");
        if (task.UserId != userId) throw new BusinessException("该任务不属于当前用户");
        if (task.Status != OaTaskStatus.Pending) throw new BusinessException("该任务已处理");
        return task;
    }

    private async Task RecallPendingTasksAsync(Guid instanceId, Guid? exceptTaskId, CancellationToken cancellationToken)
    {
        var pending = await (await _tasks.GetQueryableAsync()).Where(x => x.InstanceId == instanceId && x.Status == OaTaskStatus.Pending && (!exceptTaskId.HasValue || x.Id != exceptTaskId.Value)).ToListAsync(cancellationToken);
        foreach (var task in pending) { task.Status = OaTaskStatus.Recalled; task.CompleteTime = DateTime.UtcNow; }
        if (pending.Count > 0) await _tasks.UpdateManyAsync(pending, autoSave: true, cancellationToken: cancellationToken);
    }

    private async Task CompleteInstanceAsync(OaInstance instance, CancellationToken cancellationToken)
    {
        instance.Status = OaInstanceStatus.Approved;
        instance.EndTime = DateTime.UtcNow;
        await _instances.UpdateAsync(instance, autoSave: true, cancellationToken: cancellationToken);
    }

    private Task AddLogAsync(Guid instanceId, Guid? taskId, Guid userId, OaOperationType operation, Guid? sourceNodeId, Guid? targetNodeId, string? remark, CancellationToken cancellationToken) =>
        _logs.InsertAsync(new OaOperationLog(_ids.Create())
        {
            InstanceId = instanceId, TaskId = taskId, Operator = userId,
            OperationType = operation, SourceNodeId = sourceNodeId, TargetNodeId = targetNodeId, Remark = remark
        }, autoSave: true, cancellationToken: cancellationToken);

}
