using System.Text.Json;
using System.Security.Cryptography;
using BeaverX.Admin.Application.Contracts.Oa;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Oa;
using BeaverX.Admin.Domain.Shared.Oa;
using Microsoft.EntityFrameworkCore;
using RulesEngine.Models;
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
            DefId = definition.Id,
            Initiator = userId,
            FormValue = input.FlowValue,
            Status = OaInstanceStatus.Underway
        };
        await _instances.InsertAsync(instance, autoSave: true, cancellationToken: cancellationToken);
        await AddLogAsync(instance.Id, null, userId, OaOperationType.Start, null, null, null, cancellationToken);

        var allNodes = await (await _nodes.GetQueryableAsync()).Where(x => x.DefId == definition.Id).ToListAsync(cancellationToken);
        var start = allNodes.FirstOrDefault(x => x.NodeType == OaNodeType.Start) ?? allNodes.FirstOrDefault();
        if (start == null) throw new BusinessException("流程定义没有节点");
        await ContinueAsync(instance, start, allNodes, input.Designees, includeCurrent: true, cancellationToken);
    }

    public async Task<List<OaFlowChartNodeDto>> ViewProcessChartAsync(OaViewProcessChartRequest input, CancellationToken cancellationToken = default)
    {
        var initiator = GetCurrentUserId();
        var nodes = await (await _nodes.GetQueryableAsync()).AsNoTracking().Where(x => x.DefId == input.FlowDefId).ToListAsync(cancellationToken);
        var nodeIds = nodes.Select(x => x.Id).ToList();
        var approvers = await (await _approvers.GetQueryableAsync()).AsNoTracking().Where(x => nodeIds.Contains(x.NodeId)).ToListAsync(cancellationToken);
        var ccs = await (await _ccConfigs.GetQueryableAsync()).AsNoTracking().Where(x => nodeIds.Contains(x.NodeId)).ToListAsync(cancellationToken);
        var transactors = await (await _transactConfigs.GetQueryableAsync()).AsNoTracking().Where(x => nodeIds.Contains(x.NodeId)).ToListAsync(cancellationToken);
        using var formDocument = JsonDocument.Parse(string.IsNullOrWhiteSpace(input.FlowValue) ? "{}" : input.FlowValue);
        var form = formDocument.RootElement;
        var routeNodes = await ResolveFlowRouteAsync(
            nodes,
            gateway => SelectConditionBranchAsync(initiator, form, gateway, nodes, cancellationToken));
        var result = new List<OaFlowChartNodeDto>();
        foreach (var node in routeNodes)
        {
            var configs = node.NodeType switch
            {
                OaNodeType.Copy => ccs.Where(x => x.NodeId == node.Id)
                    .Select(x => new AssigneeConfig((OaAssigneeType)x.CcType, x.Assignees, x.Roles, x.LayerType, x.Layer)).ToList(),
                OaNodeType.Transact => transactors.Where(x => x.NodeId == node.Id)
                    .Select(x => new AssigneeConfig((OaAssigneeType)x.AssigneeType, x.Assignees, x.Roles, x.LayerType, x.Layer)).ToList(),
                _ => approvers.Where(x => x.NodeId == node.Id)
                    .Select(x => new AssigneeConfig(x.AssigneeType, x.Assignees, x.Roles, x.LayerType, x.Layer)).ToList()
            };
            var userIds = new List<Guid>();
            foreach (var config in configs.Where(x => x.AssigneeType is not OaAssigneeType.Role and not OaAssigneeType.InitiatorChoice))
            {
                userIds.AddRange(await ResolveConfiguredUsersAsync(
                    initiator,
                    config.AssigneeType,
                    config.Assignees,
                    config.Roles,
                    config.LayerType,
                    config.Layer,
                    cancellationToken));
            }

            var options = GetNodeRuntimeOptions(node);
            result.Add(new OaFlowChartNodeDto
            {
                Id = node.Id,
                NodeId = node.Id,
                Name = node.NodeName,
                NodeType = (int)node.NodeType,
                ApprovalType = node.ApprovalType,
                MultiInstanceApprovalType = node.MultiInstanceApprovalType ?? 0,
                FlowNodeNoAuditorType = node.FlowNodeNoAuditorType ?? 0,
                FlowNodeNoAuditorAssignee = node.FlowNodeNoAuditorAssignee,
                FlowNodeAuditAdmin = options.FlowNodeAuditAdmin,
                UserIds = userIds.Distinct().Select(x => x.ToString()).ToList(),
                RoleIds = configs.Where(x => x.AssigneeType == OaAssigneeType.Role)
                    .SelectMany(x => x.Roles).Where(IsGuid).Distinct().ToList(),
                InitatorChoice = configs.Any(x => x.AssigneeType == OaAssigneeType.InitiatorChoice)
            });
        }

        return result;
    }

    private static async Task<List<OaNode>> ResolveFlowRouteAsync(
        List<OaNode> nodes,
        Func<OaNode, Task<OaNode?>> selectBranch)
    {
        var nodeMap = nodes.ToDictionary(x => x.Id);
        var visited = new HashSet<Guid>();
        var route = new List<OaNode>();
        var current = nodes.FirstOrDefault(x => x.NodeType == OaNodeType.Start)
            ?? nodes.FirstOrDefault(x => !x.ParentNodeId.HasValue);
        for (var guard = 0; guard < nodes.Count + 5 && current != null && visited.Add(current.Id); guard++)
        {
            if (current.NodeType == OaNodeType.ExclusiveGateway)
            {
                current = await selectBranch(current);
                continue;
            }

            if (!current.IsConditionBranch && current.NodeType != OaNodeType.Condition && current.NodeType != OaNodeType.Trigger)
                route.Add(current);
            if (current.NodeType == OaNodeType.End) break;
            current = GetNextNode(current, nodes, nodeMap);
        }

        return route;
    }

    private static OaNode? GetNextNode(
        OaNode current,
        List<OaNode> nodes,
        IReadOnlyDictionary<Guid, OaNode>? nodeMap = null)
    {
        nodeMap ??= nodes.ToDictionary(x => x.Id);
        var direct = current.ChildNodeId.HasValue && nodeMap.TryGetValue(current.ChildNodeId.Value, out var child)
            ? child
            : null;
        if (direct is not null && direct.NodeType != OaNodeType.End) return direct;

        var branch = current.IsConditionBranch ? current : null;
        var cursor = current;
        var visited = new HashSet<Guid> { current.Id };
        while (branch == null && cursor.ParentNodeId.HasValue && nodeMap.TryGetValue(cursor.ParentNodeId.Value, out var parent) && visited.Add(parent.Id))
        {
            if (parent.IsConditionBranch) branch = parent;
            cursor = parent;
        }

        if (branch?.ParentNodeId is Guid gatewayId &&
            nodeMap.TryGetValue(gatewayId, out var gateway) &&
            gateway.ChildNodeId is Guid continuationId &&
            nodeMap.TryGetValue(continuationId, out var continuation) &&
            continuation.Id != direct?.Id)
            return continuation;

        return direct;
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
        var ccRecords = await (await _ccRecords.GetQueryableAsync()).AsNoTracking()
            .Where(x => x.InstanceId == instanceId).OrderBy(x => x.CreationTime).ToListAsync(cancellationToken);
        var logs = await (await _logs.GetQueryableAsync()).AsNoTracking()
            .Where(x => x.InstanceId == instanceId).OrderBy(x => x.CreationTime).ToListAsync(cancellationToken);
        var comments = await (await _comments.GetQueryableAsync()).AsNoTracking()
            .Where(x => x.InstanceId == instanceId).OrderBy(x => x.CreationTime).ToListAsync(cancellationToken);
        if (_currentUser.Id is { } currentUserId)
        {
            var activeTask = tasks.FirstOrDefault(x => x.UserId == currentUserId && x.Status == OaTaskStatus.Pending);
            var activeNode = activeTask == null ? null : nodes.FirstOrDefault(x => x.Id == activeTask.NodeId);
            if (activeNode != null) ApplyNodeFormAuth(fields, activeNode);
        }

        var nodeMap = nodes.ToDictionary(x => x.Id);
        var routeNodes = await ResolveFlowRouteAsync(
            nodes,
            gateway => SelectConditionBranchAsync(instance, gateway, nodes, cancellationToken));
        var historyNodes = tasks.Where(task => nodeMap.ContainsKey(task.NodeId)).Select(task =>
        {
            var node = nodeMap[task.NodeId];
            return new OaFlowInstanceNodeDto
            {
                Id = task.Id,
                Name = task.NodeName,
                FlowInstId = instanceId,
                FlowNodeId = task.NodeId,
                FlowNodeName = task.NodeName,
                UserIds = [task.UserId.ToString()],
                Underway = task.Status == OaTaskStatus.Pending,
                Type = (int)node.NodeType,
                NodeType = (int)node.NodeType,
                MultiInstanceApprovalType = node.MultiInstanceApprovalType ?? 0,
                FlowCmd = task.FlowCmd.HasValue ? (int)task.FlowCmd.Value : null,
                AuditTime = task.CompleteTime,
                Auditor = task.UserId.ToString(),
                Assignee = task.UserId.ToString(),
                Comment = task.Remark
            };
        }).ToList();

        var startNode = routeNodes.FirstOrDefault(x => x.NodeType == OaNodeType.Start);
        var startLog = logs.FirstOrDefault(x => x.OperationType == OaOperationType.Start);
        if (startNode != null && startLog != null)
            historyNodes.Add(CreateHistoryNode(instance, startNode, startLog.Id, OaOperationType.Start, startLog.CreationTime, [instance.Initiator], startLog.Operator, startLog.Remark));

        foreach (var log in logs.Where(x => x.SourceNodeId.HasValue &&
                     x.OperationType is OaOperationType.AutoApproved or OaOperationType.AutoRejected or OaOperationType.ServiceTask))
            if (nodeMap.TryGetValue(log.SourceNodeId!.Value, out var node))
                historyNodes.Add(CreateHistoryNode(instance, node, log.Id, log.OperationType, log.CreationTime, [], log.Operator, log.Remark));

        foreach (var comment in comments)
        {
            var task = comment.TaskId.HasValue ? tasks.FirstOrDefault(x => x.Id == comment.TaskId.Value) : null;
            var node = task != null && nodeMap.TryGetValue(task.NodeId, out var taskNode) ? taskNode : startNode;
            if (node == null) continue;
            historyNodes.Add(new OaFlowInstanceNodeDto
            {
                Id = comment.Id,
                Name = node.NodeName,
                FlowInstId = instanceId,
                FlowNodeId = node.Id,
                FlowNodeName = node.NodeName,
                UserIds = [comment.Commenter.ToString()],
                Underway = false,
                Type = (int)node.NodeType,
                NodeType = (int)node.NodeType,
                MultiInstanceApprovalType = node.MultiInstanceApprovalType ?? 0,
                FlowCmd = (int)OaOperationType.Comment,
                AuditTime = comment.CreationTime,
                Auditor = comment.Commenter.ToString(),
                Assignee = comment.Commenter.ToString(),
                Comment = comment.Content,
                Files = ParseCommentFiles(comment.Attachment)
            });
        }

        var copyLogs = logs.Where(x => x.SourceNodeId.HasValue && x.OperationType == OaOperationType.Copy)
            .GroupBy(x => x.SourceNodeId!.Value).ToDictionary(x => x.Key, x => x.First());
        var ccRecordsByNode = ccRecords.GroupBy(x => x.NodeId).ToDictionary(x => x.Key, x => x.ToList());
        foreach (var copyNode in routeNodes.Where(x => x.NodeType == OaNodeType.Copy))
        {
            ccRecordsByNode.TryGetValue(copyNode.Id, out var nodeRecords);
            copyLogs.TryGetValue(copyNode.Id, out var copyLog);
            if (nodeRecords is not { Count: > 0 } && copyLog == null && instance.Status != OaInstanceStatus.Approved) continue;
            var historyId = copyLog?.Id ?? nodeRecords?.First().Id ?? copyNode.Id;
            var auditTime = copyLog?.CreationTime ?? nodeRecords?.Min(x => x.CreationTime) ?? instance.EndTime;
            var users = nodeRecords?.Select(x => x.UserId).Distinct().ToList() ?? [];
            historyNodes.Add(CreateHistoryNode(instance, copyNode, historyId, OaOperationType.Copy, auditTime, users, instance.Initiator, null));
        }

        var completedNodeIds = historyNodes.Where(x => !x.Underway).Select(x => x.FlowNodeId).ToHashSet();
        var hasPendingEffectiveTask = tasks.Where(x => x.Status == OaTaskStatus.Pending).Any(x =>
            nodeMap.TryGetValue(x.NodeId, out var node) &&
            node.NodeType is OaNodeType.Approve or OaNodeType.Transact);
        var hasUnfinishedEffectiveNode = hasPendingEffectiveTask || routeNodes.Any(x =>
            (x.NodeType is OaNodeType.Approve or OaNodeType.Transact) && !completedNodeIds.Contains(x.Id));
        var endCompleted = instance.Status == OaInstanceStatus.Approved ||
            instance.Status == OaInstanceStatus.Underway && !hasUnfinishedEffectiveNode;
        if (endCompleted)
        {
            var endNode = routeNodes.LastOrDefault(x => x.NodeType == OaNodeType.End);
            if (endNode != null)
            {
                var endTime = instance.EndTime ?? historyNodes.Select(x => x.AuditTime).Max();
                historyNodes.Add(CreateHistoryNode(instance, endNode, endNode.Id, null, endTime, [], null, null));
            }
        }

        var touched = historyNodes.Select(x => x.FlowNodeId).ToHashSet();
        var future = instance.Status == OaInstanceStatus.Underway
            ? routeNodes.Where(x => !touched.Contains(x.Id) && x.NodeType != OaNodeType.Start)
            .Select(x => new OaFlowInstanceNodeDto
            {
                Id = x.Id,
                Name = x.NodeName,
                FlowInstId = instanceId,
                FlowNodeId = x.Id,
                FlowNodeName = x.NodeName,
                Type = (int)x.NodeType,
                NodeType = (int)x.NodeType,
                MultiInstanceApprovalType = x.MultiInstanceApprovalType ?? 0
            }).ToList()
            : [];
        var orderedHistory = historyNodes.OrderBy(x => x.AuditTime ?? DateTime.MaxValue).ThenBy(x => x.Id).ToList();
        return new OaFlowInstanceDetailsDto { FormValue = instance.FormValue, FormWidgets = fields, Nodes = orderedHistory, FutureNodes = future };
    }

    private static OaFlowInstanceNodeDto CreateHistoryNode(
        OaInstance instance,
        OaNode node,
        Guid id,
        OaOperationType? operation,
        DateTime? auditTime,
        IEnumerable<Guid> userIds,
        Guid? auditor,
        string? comment) => new()
        {
            Id = id,
            Name = node.NodeName,
            FlowInstId = instance.Id,
            FlowNodeId = node.Id,
            FlowNodeName = node.NodeName,
            UserIds = userIds.Select(x => x.ToString()).Distinct().ToList(),
            Underway = false,
            Type = (int)node.NodeType,
            NodeType = (int)node.NodeType,
            MultiInstanceApprovalType = node.MultiInstanceApprovalType ?? 0,
            FlowCmd = operation.HasValue ? (int)operation.Value : null,
            AuditTime = auditTime,
            Auditor = auditor?.ToString(),
            Assignee = auditor?.ToString(),
            Comment = comment
        };

    private static List<object> ParseCommentFiles(string? attachment)
    {
        if (string.IsNullOrWhiteSpace(attachment)) return [];
        try
        {
            var fileIds = JsonSerializer.Deserialize<List<string>>(attachment, WorkflowJsonOptions) ?? [];
            return fileIds.Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => (object)new { id = x, name = Path.GetFileName(x) })
                .ToList();
        }
        catch (JsonException)
        {
            return [(object)new { id = attachment, name = Path.GetFileName(attachment) }];
        }
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
            FlowDefId = definition.Id,
            Name = definition.Name,
            GroupId = definition.GroupId,
            Cancelable = definition.Cancelable,
            Id = instance.Id,
            InstanceNo = instance.InstanceNo,
            InitiatorId = instance.Initiator.ToString(),
            BeginTime = instance.CreationTime,
            EndTime = instance.EndTime,
            Status = (int)instance.Status,
            TaskId = task?.Id,
            ActNodeId = task?.NodeId,
            Assignable = node?.Assignable ?? false,
            Signable = node?.Signable ?? false,
            Backable = node?.Backable ?? false,
            Signature = node?.Signature ?? false,
            NodeType = node == null ? 0 : (int)node.NodeType,
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
                CreatorId = x.Operator.ToString(),
                CreateTime = x.CreationTime,
                FormValue = x.Remark ?? "{}"
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
            InstanceId = input.InstanceId,
            TaskId = input.TaskId,
            Commenter = userId,
            Content = input.Content.Trim(),
            Attachment = input.Attachment
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
                    InstanceId = instance.Id,
                    NodeId = node.Id,
                    NodeName = node.NodeName,
                    UserId = Guid.Parse(nextUser),
                    Status = OaTaskStatus.Pending,
                    ParentTaskId = task.Id,
                    CandidateUsers = remainingCandidates,
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
        var next = GetNextNode(node, allNodes);
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
            InstanceId = task.InstanceId,
            NodeId = task.NodeId,
            NodeName = task.NodeName,
            UserId = assignee,
            Status = OaTaskStatus.Pending,
            ParentTaskId = task.Id
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
            InstanceId = task.InstanceId,
            NodeId = task.NodeId,
            NodeName = task.NodeName,
            UserId = userId,
            Status = OaTaskStatus.Pending,
            ParentTaskId = task.Id,
            FlowCmd = OaOperationType.AddSign
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
        var node = includeCurrent ? current : GetNextNode(current, allNodes);
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
                node = GetNextNode(node, allNodes);
                continue;
            }
            if (node.NodeType == OaNodeType.Copy)
            {
                await CreateCcRecordsAsync(instance, node, cancellationToken);
                node = GetNextNode(node, allNodes);
                continue;
            }
            if (node.NodeType == OaNodeType.ServiceTask)
            {
                await ExecuteServiceTaskAsync(instance, node, cancellationToken);
                node = GetNextNode(node, allNodes);
                continue;
            }
            if ((node.NodeType == OaNodeType.Approve || node.NodeType == OaNodeType.Transact) && node.ApprovalType == 1)
            {
                await AddLogAsync(instance.Id, null, instance.Initiator, OaOperationType.AutoApproved, node.Id, node.ChildNodeId, null, cancellationToken);
                node = GetNextNode(node, allNodes);
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
                    node = GetNextNode(node, allNodes);
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
                        node = GetNextNode(node, allNodes);
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
                    InstanceId = instance.Id,
                    NodeId = node.Id,
                    NodeName = node.NodeName,
                    UserId = userId,
                    Status = OaTaskStatus.Pending,
                    CandidateUsers = candidates,
                    LoopCounter = node.MultiInstanceApprovalType == 3 ? 0 : null
                }).ToList();
                await _tasks.InsertManyAsync(tasks, autoSave: true, cancellationToken: cancellationToken);
                return;
            }
            node = GetNextNode(node, allNodes);
        }
        if (node == null) await CompleteInstanceAsync(instance, cancellationToken);
    }

    private async Task ExecuteServiceTaskAsync(OaInstance instance, OaNode node, CancellationToken cancellationToken)
    {
        var keys = GetNodeRuntimeOptions(node).ServiceTaskHandlers;
        if (keys.Count == 0) throw new BusinessException($"服务任务节点“{node.NodeName}”未配置处理器");

        using var formDocument = JsonDocument.Parse(instance.FormValue);
        var context = new OaServiceTaskContext
        {
            InstanceId = instance.Id,
            DefinitionId = instance.DefId,
            NodeId = node.Id,
            InitiatorId = instance.Initiator,
            FormData = formDocument.RootElement.EnumerateObject()
                .ToDictionary(x => x.Name, x => ToServiceTaskFormValue(x.Value), StringComparer.Ordinal)
        };
        foreach (var key in keys)
        {
            if (!_serviceTaskHandlers.TryGetValue(key, out var handler))
                throw new BusinessException($"服务任务节点“{node.NodeName}”的处理器“{key}”不可用");
            await handler.HandleAsync(context, cancellationToken);
        }

        await AddLogAsync(instance.Id, null, instance.Initiator, OaOperationType.ServiceTask,
            node.Id, node.ChildNodeId, $"已执行处理器：{string.Join("、", keys)}", cancellationToken);
    }

    private static object? ToServiceTaskFormValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
        JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
        JsonValueKind.Number => value.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.Array => value.EnumerateArray().Select(ToServiceTaskFormValue).ToList(),
        JsonValueKind.Object => value.EnumerateObject()
            .ToDictionary(x => x.Name, x => ToServiceTaskFormValue(x.Value), StringComparer.Ordinal),
        _ => null
    };

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
        var existing = await (await _ccRecords.GetQueryableAsync()).AsNoTracking()
            .Where(x => x.InstanceId == instance.Id && x.NodeId == node.Id)
            .Select(x => x.UserId).ToListAsync(cancellationToken);
        var records = users.Distinct().Except(existing).Select(userId => new OaCcRecord(_ids.Create()) { InstanceId = instance.Id, NodeId = node.Id, UserId = userId }).ToList();
        if (records.Count > 0) await _ccRecords.InsertManyAsync(records, autoSave: true, cancellationToken: cancellationToken);
        if (!await _logs.AnyAsync(x => x.InstanceId == instance.Id && x.SourceNodeId == node.Id && x.OperationType == OaOperationType.Copy, cancellationToken))
            await AddLogAsync(instance.Id, null, instance.Initiator, OaOperationType.Copy, node.Id, node.ChildNodeId, null, cancellationToken);
    }

    private async Task<OaNode?> SelectConditionBranchAsync(OaInstance instance, OaNode gateway, List<OaNode> allNodes, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(instance.FormValue);
        return await SelectConditionBranchAsync(instance.Initiator, document.RootElement, gateway, allNodes, cancellationToken);
    }

    private async Task<OaNode?> SelectConditionBranchAsync(
        Guid initiator,
        JsonElement form,
        OaNode gateway,
        List<OaNode> allNodes,
        CancellationToken cancellationToken)
    {
        var graph = await LoadGatewayConditionsAsync(gateway, allNodes, cancellationToken);
        var identities = await GetInitiatorIdentitiesAsync(initiator, cancellationToken);
        var branch = await SelectMatchingBranchAsync(
            graph,
            condition => MatchCondition(identities, form, condition),
            branch => EvaluateConditionExpressionAsync(branch.ConditionExpression, identities, form));
        return branch == null ? GetNextNode(gateway, allNodes) : GetNextNode(branch, allNodes);
    }

    private async Task<GatewayConditionGraph> LoadGatewayConditionsAsync(
        OaNode gateway,
        List<OaNode> allNodes,
        CancellationToken cancellationToken)
    {
        var branches = allNodes.Where(x => x.ParentNodeId == gateway.Id && x.IsConditionBranch)
            .OrderBy(x => x.PriorityLevel ?? int.MaxValue).ThenBy(x => x.Id).ToList();
        var branchIds = branches.Select(x => x.Id).ToList();
        var groups = await (await _conditionGroups.GetQueryableAsync()).AsNoTracking()
            .Where(x => branchIds.Contains(x.NodeId)).ToListAsync(cancellationToken);
        var groupIds = groups.Select(x => x.Id).ToList();
        var conditions = await (await _conditions.GetQueryableAsync()).AsNoTracking()
            .Where(x => groupIds.Contains(x.GroupId)).ToListAsync(cancellationToken);
        return new GatewayConditionGraph(branches, groups, conditions);
    }

    private static async Task<OaNode?> SelectMatchingBranchAsync(
        GatewayConditionGraph graph,
        Func<OaCondition, bool> matchesCondition,
        Func<OaNode, Task<bool>>? matchesExpression = null)
    {
        var groupsByBranch = graph.Groups.ToLookup(x => x.NodeId);
        var conditionsByGroup = graph.Conditions.ToLookup(x => x.GroupId);
        OaNode? fallback = null;
        foreach (var branch in graph.Branches)
        {
            if (!string.IsNullOrWhiteSpace(branch.ConditionExpression) && matchesExpression != null)
            {
                if (await matchesExpression(branch)) return branch;
            }

            var branchGroups = groupsByBranch[branch.Id].ToList();
            var groupsWithConditions = branchGroups
                .Select(group => conditionsByGroup[group.Id].ToList())
                .Where(conditions => conditions.Count > 0)
                .ToList();
            if (groupsWithConditions.Count == 0)
            {
                fallback ??= branch;
                continue;
            }
            if (groupsWithConditions.Any(conditions => conditions.All(matchesCondition))) return branch;
        }
        return fallback;
    }

    private sealed record GatewayConditionGraph(
        List<OaNode> Branches,
        List<OaConditionGroup> Groups,
        List<OaCondition> Conditions);

    private static async Task<bool> EvaluateConditionExpressionAsync(
        string? expression,
        HashSet<string> initiatorIdentities,
        JsonElement? form)
    {
        if (string.IsNullOrWhiteSpace(expression)) return false;
        var workflowName = $"OaCondition_{Guid.NewGuid():N}";
        var workflow = new Workflow
        {
            WorkflowName = workflowName,
            Rules =
            [
                new Rule
                {
                    RuleName = "Branch",
                    RuleExpressionType = RuleExpressionType.LambdaExpression,
                    Expression = expression
                }
            ]
        };

        try
        {
            var engine = new global::RulesEngine.RulesEngine([workflow]);
            var results = await engine.ExecuteAllRulesAsync(
                workflowName,
                BuildRuleParameters(initiatorIdentities, form));
            return results.Any(x => x.IsSuccess);
        }
        catch
        {
            return false;
        }
    }

    private static RuleParameter[] BuildRuleParameters(HashSet<string> initiatorIdentities, JsonElement? form)
    {
        var parameters = new List<RuleParameter>
        {
            new("initiator", initiatorIdentities.ToList())
        };
        if (!form.HasValue || form.Value.ValueKind != JsonValueKind.Object) return parameters.ToArray();

        foreach (var property in form.Value.EnumerateObject())
        {
            if (property.Name == "initiator" || !IsValidRuleParameterName(property.Name)) continue;
            parameters.Add(new RuleParameter(property.Name, ToRuleValue(property.Value)));
        }
        return parameters.ToArray();
    }

    private static bool IsValidRuleParameterName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !(char.IsLetter(name[0]) || name[0] == '_')) return false;
        return name.Skip(1).All(ch => char.IsLetterOrDigit(ch) || ch == '_');
    }

    private static object ToRuleValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
            JsonValueKind.Number => value.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => ToRuleArray(value),
            JsonValueKind.Object when value.TryGetProperty("id", out var id) => ToRuleValue(id),
            JsonValueKind.Object => JsonSerializer.Deserialize<Dictionary<string, object?>>(value.GetRawText()) ?? [],
            _ => string.Empty
        };
    }

    private static object ToRuleArray(JsonElement value)
    {
        var items = value.EnumerateArray().ToList();
        if (items.All(x => x.ValueKind == JsonValueKind.String))
            return items.Select(x => x.GetString() ?? string.Empty).ToList();
        if (items.All(x => x.ValueKind == JsonValueKind.Number && x.TryGetDecimal(out _)))
            return items.Select(x => x.GetDecimal()).ToList();
        if (items.All(x => x.ValueKind == JsonValueKind.Object && x.TryGetProperty("id", out _)))
            return items.Select(x => ToRuleValue(x.GetProperty("id"))).ToList();
        return items.Select(ToRuleValue).ToList();
    }

    private static bool MatchInitiatorCondition(HashSet<string> initiatorIdentities, OaCondition condition)
    {
        var contains = (condition.Values ?? []).Any(initiatorIdentities.Contains);
        return condition.Operator switch
        {
            20 => contains,
            21 => !contains,
            _ => false
        };
    }

    private static bool MatchCondition(HashSet<string> initiatorIdentities, JsonElement form, OaCondition condition)
    {
        var expected = condition.Values ?? [];
        if (condition.VarName == "initiator")
            return MatchInitiatorCondition(initiatorIdentities, condition);
        var actualValues = GetConditionValues(form, condition.VarName);
        if (actualValues.Count == 0) return false;
        var actualText = actualValues.FirstOrDefault() ?? string.Empty;
        var expectedText = expected.FirstOrDefault() ?? string.Empty;
        if (condition.Operator is >= 0 and <= 5 &&
            decimal.TryParse(expectedText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var right))
            return actualValues
                .Select(x => decimal.TryParse(x, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var left) ? (decimal?)left : null)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Any(left => condition.Operator switch
            {
                0 => left == right,
                1 => left != right,
                2 => left < right,
                3 => left <= right,
                4 => left > right,
                5 => left >= right,
                _ => false
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
            InstanceId = instanceId,
            TaskId = taskId,
            Operator = userId,
            OperationType = operation,
            SourceNodeId = sourceNodeId,
            TargetNodeId = targetNodeId,
            Remark = remark
        }, autoSave: true, cancellationToken: cancellationToken);

}
