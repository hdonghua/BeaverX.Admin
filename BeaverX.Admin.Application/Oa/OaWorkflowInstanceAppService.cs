using System.Text.Json;
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
        EnsureJsonObject(input.FlowValue);

        var instance = new OaInstance(_ids.Create())
        {
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
        var configs = await (await _approvers.GetQueryableAsync()).AsNoTracking().Where(x => nodeIds.Contains(x.NodeId)).ToListAsync(cancellationToken);
        return nodes.Where(x => !x.IsConditionBranch).Select(node => new OaFlowChartNodeDto
        {
            Id = node.Id, NodeId = node.Id, Name = node.NodeName, NodeType = (int)node.NodeType,
            ApprovalType = node.ApprovalType, MultiInstanceApprovalType = node.MultiInstanceApprovalType ?? 0,
            FlowNodeNoAuditorType = node.FlowNodeNoAuditorType ?? 0,
            UserIds = configs.Where(x => x.NodeId == node.Id && x.AssigneeType != OaAssigneeType.Role).SelectMany(x => x.Assignees).Distinct().ToList(),
            RoleIds = configs.Where(x => x.NodeId == node.Id && x.AssigneeType == OaAssigneeType.Role).SelectMany(x => x.Roles.Count > 0 ? x.Roles : x.Assignees).Distinct().ToList(),
            InitatorChoice = configs.Any(x => x.NodeId == node.Id && x.AssigneeType == OaAssigneeType.InitiatorChoice)
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
        return new OaFlowInstanceListDto
        {
            FlowDefId = definition.Id, Name = definition.Name, GroupId = definition.GroupId, Cancelable = definition.Cancelable,
            Id = instance.Id, InitiatorId = instance.Initiator.ToString(), BeginTime = instance.CreationTime, EndTime = instance.EndTime,
            Status = (int)instance.Status, TaskId = task?.Id, ActNodeId = task?.NodeId,
            Assignable = node?.Assignable ?? false, Signable = node?.Signable ?? false, Backable = node?.Backable ?? false,
            Signature = node?.Signature ?? false, NodeType = node == null ? 0 : (int)node.NodeType, Summary = BuildSummary(instance.FormValue)
        };
    }

    public async Task FormModifyAsync(OaFormModifyRequest input, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var task = await GetPendingTaskAsync(input.TaskId, userId, cancellationToken);
        if (task.InstanceId != input.FlowInstId || task.NodeId != input.FlowNodeId) throw new BusinessException("流程任务信息不匹配");
        EnsureJsonObject(input.FlowValue);
        var instance = await _instances.GetAsync(input.FlowInstId, cancellationToken: cancellationToken);
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
                if (users.Count == 0)
                {
                    if (node.FlowNodeNoAuditorType == 1 && Guid.TryParse(node.FlowNodeNoAuditorAssignee, out var fallback)) users.Add(fallback);
                    else
                    {
                        await AddLogAsync(instance.Id, null, instance.Initiator, OaOperationType.AutoApproved, node.Id, node.ChildNodeId, "审批人为空，自动通过", cancellationToken);
                        node = node.ChildNodeId.HasValue ? allNodes.FirstOrDefault(x => x.Id == node.ChildNodeId.Value) : null;
                        continue;
                    }
                }
                if (node.FlowNodeSelfAuditorType == 1) users.Remove(instance.Initiator);
                if (users.Count == 0)
                {
                    node = node.ChildNodeId.HasValue ? allNodes.FirstOrDefault(x => x.Id == node.ChildNodeId.Value) : null;
                    continue;
                }
                var tasks = users.Distinct().Select(userId => new OaTask(_ids.Create())
                {
                    InstanceId = instance.Id, NodeId = node.Id, NodeName = node.NodeName,
                    UserId = userId, Status = OaTaskStatus.Pending
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
        var configs = await (await _approvers.GetQueryableAsync()).AsNoTracking().Where(x => x.NodeId == node.Id).ToListAsync(cancellationToken);
        var result = new HashSet<Guid>();
        foreach (var config in configs)
        {
            if (config.AssigneeType == OaAssigneeType.Self) result.Add(instance.Initiator);
            else if (config.AssigneeType == OaAssigneeType.Assignee)
                AddGuids(result, config.Assignees);
            else if (config.AssigneeType == OaAssigneeType.InitiatorChoice && designees?.TryGetValue(node.Id, out var selected) == true)
                AddGuids(result, selected);
            else if (config.AssigneeType == OaAssigneeType.Role)
            {
                var roleIds = (config.Roles.Count > 0 ? config.Roles : config.Assignees).Where(IsGuid).Select(Guid.Parse).ToList();
                if (roleIds.Count > 0)
                {
                    var users = await (await _userRoles.GetQueryableAsync()).AsNoTracking().Where(x => roleIds.Contains(x.RoleId)).Select(x => x.UserId).ToListAsync(cancellationToken);
                    foreach (var user in users) result.Add(user);
                }
            }
            else if (config.AssigneeType is OaAssigneeType.DepartmentLeader or OaAssigneeType.Superior or OaAssigneeType.MultistepLeader or OaAssigneeType.MultistepDepartmentLeader)
            {
                var deptIds = await (await _userDepartments.GetQueryableAsync()).AsNoTracking().Where(x => x.UserId == instance.Initiator).Select(x => x.DepartmentId).ToListAsync(cancellationToken);
                var leaders = await (await _departments.GetQueryableAsync()).AsNoTracking().Where(x => deptIds.Contains(x.Id) && x.LeaderUserId.HasValue).Select(x => x.LeaderUserId!.Value).ToListAsync(cancellationToken);
                foreach (var leader in leaders) result.Add(leader);
            }
        }
        return result.ToList();
    }

    private async Task CreateCcRecordsAsync(OaInstance instance, OaNode node, CancellationToken cancellationToken)
    {
        var configs = await (await _ccConfigs.GetQueryableAsync()).AsNoTracking().Where(x => x.NodeId == node.Id).ToListAsync(cancellationToken);
        var users = new HashSet<Guid>();
        foreach (var config in configs)
        {
            if (config.CcType == 0) users.Add(instance.Initiator);
            else AddGuids(users, config.Assignees);
            if (config.CcType == 3)
            {
                var roleIds = (config.Roles.Count > 0 ? config.Roles : config.Assignees).Where(IsGuid).Select(Guid.Parse).ToList();
                var roleUsers = await (await _userRoles.GetQueryableAsync()).AsNoTracking().Where(x => roleIds.Contains(x.RoleId)).Select(x => x.UserId).ToListAsync(cancellationToken);
                foreach (var roleUser in roleUsers) users.Add(roleUser);
            }
        }
        var existing = await (await _ccRecords.GetQueryableAsync()).AsNoTracking().Where(x => x.InstanceId == instance.Id).Select(x => x.UserId).ToListAsync(cancellationToken);
        var records = users.Except(existing).Select(userId => new OaCcRecord(_ids.Create()) { InstanceId = instance.Id, NodeId = node.Id, UserId = userId }).ToList();
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
        using var document = JsonDocument.Parse(instance.FormValue);
        return groups.Any(group => conditions.Where(x => x.GroupId == group.Id).All(condition => MatchCondition(instance, document.RootElement, condition)));
    }

    private static bool MatchCondition(OaInstance instance, JsonElement form, OaCondition condition)
    {
        var expected = condition.Values ?? [];
        if (condition.VarName == "initiator")
        {
            var contains = expected.Contains(instance.Initiator.ToString(), StringComparer.OrdinalIgnoreCase);
            return condition.Operator == 20 ? contains : !contains;
        }
        if (!form.TryGetProperty(condition.VarName, out var actual)) return false;
        var actualValues = actual.ValueKind == JsonValueKind.Array ? actual.EnumerateArray().Select(JsonValue).ToList() : [JsonValue(actual)];
        var actualText = actualValues.FirstOrDefault() ?? string.Empty;
        var expectedText = expected.FirstOrDefault() ?? string.Empty;
        if (condition.Operator is >= 0 and <= 5 && decimal.TryParse(actualText, out var left) && decimal.TryParse(expectedText, out var right))
            return condition.Operator switch { 0 => left == right, 1 => left != right, 2 => left < right, 3 => left <= right, 4 => left > right, 5 => left >= right, _ => false };
        return condition.Operator switch
        {
            10 => actualText.Contains(expectedText, StringComparison.OrdinalIgnoreCase),
            11 => !actualText.Contains(expectedText, StringComparison.OrdinalIgnoreCase),
            12 => string.Equals(actualText, expectedText, StringComparison.OrdinalIgnoreCase),
            13 => !string.Equals(actualText, expectedText, StringComparison.OrdinalIgnoreCase),
            14 => expected.Any(x => actualText.Contains(x, StringComparison.OrdinalIgnoreCase)),
            15 => expected.All(x => !actualText.Contains(x, StringComparison.OrdinalIgnoreCase)),
            20 => actualValues.Intersect(expected, StringComparer.OrdinalIgnoreCase).Any(),
            21 => !actualValues.Intersect(expected, StringComparer.OrdinalIgnoreCase).Any(),
            _ => false
        };
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
