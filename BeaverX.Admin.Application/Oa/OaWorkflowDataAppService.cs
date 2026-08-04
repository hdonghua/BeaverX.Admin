using System.Text.Json;
using System.Text.Json.Nodes;
using BeaverX.Admin.Application.Contracts.Oa;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Oa;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared.Oa;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories;

namespace BeaverX.Admin.Application.Oa;

public partial class OaWorkflowAppService
{
    public Task<PagedResultDto<OaFlowInstanceListDto>> QueryInstancesAsync(OaFlowInstanceQuery input, CancellationToken cancellationToken = default) =>
        QueryInstancePageAsync(input, null, cancellationToken);

    public async Task TransferAsync(OaTransferRequest input, CancellationToken cancellationToken = default)
    {
        var operatorId = GetCurrentUserId();
        var instance = await _instances.GetAsync(input.FlowInstId, cancellationToken: cancellationToken);
        var definition = await _definitions.GetAsync(instance.DefId, cancellationToken: cancellationToken);
        EnsureFlowAdmin(definition);
        var oldUserId = ParseUserId(input.AssigneeId, "原办理人无效");
        var newUserId = ParseUserId(input.NewAssigneeId, "新办理人无效");
        var tasks = await (await _tasks.GetQueryableAsync()).Where(x => x.InstanceId == instance.Id && x.UserId == oldUserId && x.Status == OaTaskStatus.Pending).ToListAsync(cancellationToken);
        if (tasks.Count == 0) throw new BusinessException("原办理人没有待办任务");
        foreach (var task in tasks) task.UserId = newUserId;
        await _tasks.UpdateManyAsync(tasks, autoSave: true, cancellationToken: cancellationToken);
        await AddLogAsync(instance.Id, null, operatorId, OaOperationType.Transfer, null, null, input.Comment, cancellationToken);
    }

    private async Task<PagedResultDto<OaFlowInstanceListDto>> QueryInstancePageAsync(OaFlowInstanceQuery input, QueryScope? scope, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var instanceQuery = (await _instances.GetQueryableAsync()).AsNoTracking();
        var definitionQuery = (await _definitions.GetQueryableAsync()).AsNoTracking();
        var taskQuery = (await _tasks.GetQueryableAsync()).AsNoTracking();
        var ccQuery = (await _ccRecords.GetQueryableAsync()).AsNoTracking();

        if (scope == QueryScope.Mine) instanceQuery = instanceQuery.Where(x => x.Initiator == currentUserId);
        if (scope == QueryScope.Pending)
        {
            var ids = taskQuery.Where(x => x.UserId == currentUserId && x.Status == OaTaskStatus.Pending).Select(x => x.InstanceId);
            instanceQuery = instanceQuery.Where(x => ids.Contains(x.Id));
        }
        if (scope == QueryScope.Audited)
        {
            var ids = taskQuery.Where(x => x.UserId == currentUserId && x.Status != OaTaskStatus.Pending).Select(x => x.InstanceId);
            instanceQuery = instanceQuery.Where(x => ids.Contains(x.Id));
        }
        if (scope == QueryScope.Cc)
        {
            var ids = ccQuery.Where(x => x.UserId == currentUserId).Select(x => x.InstanceId);
            instanceQuery = instanceQuery.Where(x => ids.Contains(x.Id));
        }
        if (input.Status.HasValue) instanceQuery = instanceQuery.Where(x => (int)x.Status == input.Status.Value);
        if (input.BeginMinTime.HasValue) instanceQuery = instanceQuery.Where(x => x.CreationTime >= input.BeginMinTime.Value);
        if (input.BeginMaxTime.HasValue) instanceQuery = instanceQuery.Where(x => x.CreationTime <= input.BeginMaxTime.Value);
        if (input.EndMinTime.HasValue) instanceQuery = instanceQuery.Where(x => x.EndTime >= input.EndMinTime.Value);
        if (input.EndMaxTime.HasValue) instanceQuery = instanceQuery.Where(x => x.EndTime <= input.EndMaxTime.Value);
        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            var defIds = definitionQuery.Where(x => x.Name.Contains(keyword)).Select(x => x.Id);
            instanceQuery = instanceQuery.Where(x => x.InstanceNo.Contains(keyword) || defIds.Contains(x.DefId));
        }

        var total = await instanceQuery.LongCountAsync(cancellationToken);
        var page = Math.Max(1, input.Page);
        var pageSize = Math.Clamp(input.PageSize, 1, 200);
        var instances = await instanceQuery.OrderByDescending(x => x.CreationTime).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var defIdList = instances.Select(x => x.DefId).Distinct().ToList();
        var defs = await definitionQuery.Where(x => defIdList.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        var instanceIds = instances.Select(x => x.Id).ToList();
        var tasks = await taskQuery.Where(x => instanceIds.Contains(x.InstanceId)).OrderByDescending(x => x.CreationTime).ToListAsync(cancellationToken);
        var nodeIds = tasks.Select(x => x.NodeId).Distinct().ToList();
        var nodes = await (await _nodes.GetQueryableAsync()).AsNoTracking().Where(x => nodeIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        var summaryFields = await (await _fields.GetQueryableAsync()).AsNoTracking()
            .Where(x => defIdList.Contains(x.DefId) && x.IsSummary).OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        var summaryFieldsByDef = summaryFields.GroupBy(x => x.DefId)
            .ToDictionary(x => x.Key, x => (IReadOnlyCollection<OaFormField>)x.ToList());

        var items = instances.Select(instance =>
        {
            var definition = defs[instance.DefId];
            var relevantTask = scope switch
            {
                QueryScope.Pending => tasks.FirstOrDefault(x => x.InstanceId == instance.Id && x.UserId == currentUserId && x.Status == OaTaskStatus.Pending),
                QueryScope.Audited => tasks.FirstOrDefault(x => x.InstanceId == instance.Id && x.UserId == currentUserId && x.Status != OaTaskStatus.Pending),
                _ => tasks.FirstOrDefault(x => x.InstanceId == instance.Id && x.Status == OaTaskStatus.Pending) ?? tasks.FirstOrDefault(x => x.InstanceId == instance.Id)
            };
            var node = relevantTask != null && nodes.TryGetValue(relevantTask.NodeId, out var found) ? found : null;
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
                TaskId = relevantTask?.Id,
                ActNodeId = relevantTask?.NodeId,
                Assignable = node?.Assignable ?? false,
                Signable = node?.Signable ?? false,
                Backable = node?.Backable ?? false,
                Signature = node?.Signature ?? false,
                NodeType = node == null ? 0 : (int)node.NodeType,
                Summary = BuildSummary(instance.FormValue,
                    summaryFieldsByDef.GetValueOrDefault(instance.DefId) ?? Array.Empty<OaFormField>())
            };
        }).ToList();
        return new PagedResultDto<OaFlowInstanceListDto> { Total = total, Items = items };
    }

    private void EnsureFlowAdmin(OaProcessDefinition definition)
    {
        var userId = GetCurrentUserId().ToString();
        if (definition.FlowAdminIds.Count > 0 && !definition.FlowAdminIds.Contains(userId)) throw new BusinessException("当前用户不是流程管理员");
    }

}
