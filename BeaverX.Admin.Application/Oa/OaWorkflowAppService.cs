using System.Text.Json;
using System.Text.Json.Nodes;
using BeaverX.Admin.Application.Contracts.Oa;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Oa;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared.Oa;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace BeaverX.Admin.Application.Oa;

public class OaWorkflowAppService : IOaWorkflowAppService, IScopedDependency
{
    private readonly IRepository<OaProcessGroup, Guid> _groups;
    private readonly IRepository<OaProcessDefinition, Guid> _definitions;
    private readonly IRepository<OaFormField, Guid> _fields;
    private readonly IRepository<OaInitiator, Guid> _initiators;
    private readonly IRepository<OaNode, Guid> _nodes;
    private readonly IRepository<OaConditionGroup, Guid> _conditionGroups;
    private readonly IRepository<OaCondition, Guid> _conditions;
    private readonly IRepository<OaApproverConfig, Guid> _approvers;
    private readonly IRepository<OaCcConfig, Guid> _ccConfigs;
    private readonly IRepository<OaInstance, Guid> _instances;
    private readonly IRepository<OaTask, Guid> _tasks;
    private readonly IRepository<OaCcRecord, Guid> _ccRecords;
    private readonly IRepository<OaComment, Guid> _comments;
    private readonly IRepository<OaOperationLog, Guid> _logs;
    private readonly IRepository<UserRole, Guid> _userRoles;
    private readonly IRepository<OaDepartment, Guid> _departments;
    private readonly IRepository<OaUserDepartment, Guid> _userDepartments;
    private readonly ICurrentUser _currentUser;
    private readonly OaIdGenerator _ids;

    public OaWorkflowAppService(
        IRepository<OaProcessGroup, Guid> groups,
        IRepository<OaProcessDefinition, Guid> definitions,
        IRepository<OaFormField, Guid> fields,
        IRepository<OaInitiator, Guid> initiators,
        IRepository<OaNode, Guid> nodes,
        IRepository<OaConditionGroup, Guid> conditionGroups,
        IRepository<OaCondition, Guid> conditions,
        IRepository<OaApproverConfig, Guid> approvers,
        IRepository<OaCcConfig, Guid> ccConfigs,
        IRepository<OaInstance, Guid> instances,
        IRepository<OaTask, Guid> tasks,
        IRepository<OaCcRecord, Guid> ccRecords,
        IRepository<OaComment, Guid> comments,
        IRepository<OaOperationLog, Guid> logs,
        IRepository<UserRole, Guid> userRoles,
        IRepository<OaDepartment, Guid> departments,
        IRepository<OaUserDepartment, Guid> userDepartments,
        ICurrentUser currentUser,
        OaIdGenerator ids)
    {
        _groups = groups;
        _definitions = definitions;
        _fields = fields;
        _initiators = initiators;
        _nodes = nodes;
        _conditionGroups = conditionGroups;
        _conditions = conditions;
        _approvers = approvers;
        _ccConfigs = ccConfigs;
        _instances = instances;
        _tasks = tasks;
        _ccRecords = ccRecords;
        _comments = comments;
        _logs = logs;
        _userRoles = userRoles;
        _departments = departments;
        _userDepartments = userDepartments;
        _currentUser = currentUser;
        _ids = ids;
    }

    public async Task<List<OaFlowGroupDto>> GetGroupsWithDefinitionsAsync(
        OaFlowGroupQuery input,
        bool onlyEnabled = false,
        CancellationToken cancellationToken = default)
    {
        var groupQuery = (await _groups.GetQueryableAsync()).AsNoTracking();
        if (onlyEnabled) groupQuery = groupQuery.Where(x => x.Status == 1);
        if (!string.IsNullOrWhiteSpace(input.Name)) groupQuery = groupQuery.Where(x => x.Name.Contains(input.Name.Trim()));

        var groupList = await groupQuery.OrderBy(x => x.SortOrder).ThenBy(x => x.CreationTime).ToListAsync(cancellationToken);
        var groupIds = groupList.Select(x => x.Id).ToList();
        var definitionQuery = (await _definitions.GetQueryableAsync()).AsNoTracking()
            .Where(x => groupIds.Contains(x.GroupId) && x.Status != OaDefinitionStatus.Draft);
        if (onlyEnabled) definitionQuery = definitionQuery.Where(x => x.Status == OaDefinitionStatus.Published);
        var allDefinitions = await definitionQuery.OrderByDescending(x => x.Version).ToListAsync(cancellationToken);
        var definitionList = allDefinitions.GroupBy(x => x.BelongKey).Select(x => x.First()).ToList();
        var defIds = definitionList.Select(x => x.Id).ToList();
        var initiatorList = await (await _initiators.GetQueryableAsync()).AsNoTracking()
            .Where(x => defIds.Contains(x.DefId)).ToListAsync(cancellationToken);
        var currentUserId = _currentUser.Id?.ToString();

        return groupList.Select(group => new OaFlowGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            FlowDefinitions = definitionList.Where(x => x.GroupId == group.Id).Select(def => new OaFlowDefinitionDto
            {
                Id = def.Id,
                LinkId = def.BelongKey,
                Name = def.Name,
                Icon = def.Icon,
                InitiatorType = (int)def.PermissionType,
                Version = def.Version,
                GroupId = def.GroupId,
                Cancelable = def.Cancelable,
                Status = def.Status == OaDefinitionStatus.Published ? 0 : 1,
                Editable = currentUserId != null && (def.FlowAdminIds.Count == 0 || def.FlowAdminIds.Contains(currentUserId)),
                FlowInitiators = initiatorList.Where(x => x.DefId == def.Id)
                    .SelectMany(x => x.InitiatorIds.Select(id => new OaFlowInitiatorRequest { Id = id, Type = x.InitiatorType })).ToList()
            }).ToList()
        }).ToList();
    }

    public async Task<OaProcessGroupDto> AddGroupAsync(OaAddProcessGroupRequest input, CancellationToken cancellationToken = default)
    {
        var name = input.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessException("流程分组名称不能为空");
        if (await _groups.AnyAsync(x => x.Name == name, cancellationToken)) throw new BusinessException("流程分组已存在");
        var entity = new OaProcessGroup(_ids.Create()) { Name = name, SortOrder = 0, Status = 1 };
        await _groups.InsertAsync(entity, autoSave: true, cancellationToken: cancellationToken);
        return new OaProcessGroupDto { Id = entity.Id, Name = entity.Name };
    }

    public async Task<OaProcessGroupDto> UpdateGroupAsync(OaUpdateProcessGroupRequest input, CancellationToken cancellationToken = default)
    {
        var name = input.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessException("流程分组名称不能为空");
        var entity = await _groups.GetAsync(input.Id, cancellationToken: cancellationToken);
        if (await _groups.AnyAsync(x => x.Id != input.Id && x.Name == name, cancellationToken))
            throw new BusinessException("流程分组已存在");
        entity.Name = name;
        await _groups.UpdateAsync(entity, autoSave: true, cancellationToken: cancellationToken);
        return new OaProcessGroupDto { Id = entity.Id, Name = entity.Name };
    }

    public async Task DeleteGroupAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (await _definitions.AnyAsync(x => x.GroupId == id, cancellationToken))
            throw new BusinessException("请先删除分组中的流程");
        await _groups.DeleteAsync(id, autoSave: true, cancellationToken: cancellationToken);
    }

    public async Task<List<OaProcessGroupDto>> GetGroupsAsync(CancellationToken cancellationToken = default) =>
        await (await _groups.GetQueryableAsync()).AsNoTracking().Where(x => x.Status == 1)
            .OrderBy(x => x.SortOrder).Select(x => new OaProcessGroupDto { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);

    public async Task AddProcessAsync(OaAddProcessRequest input, CancellationToken cancellationToken = default) =>
        await SaveProcessAsync(input, null, cancellationToken);

    public async Task UpdateProcessAsync(OaAddProcessRequest input, CancellationToken cancellationToken = default)
    {
        if (!input.WorkFlowDef.Id.HasValue) throw new BusinessException("流程定义 ID 不能为空");
        var old = await _definitions.GetAsync(input.WorkFlowDef.Id.Value, cancellationToken: cancellationToken);
        EnsureFlowAdmin(old);
        old.Status = OaDefinitionStatus.Disabled;
        await _definitions.UpdateAsync(old, autoSave: true, cancellationToken: cancellationToken);
        await SaveProcessAsync(input, old, cancellationToken);
    }

    private async Task<OaProcessDefinition> SaveProcessAsync(OaAddProcessRequest input, OaProcessDefinition? previous, CancellationToken cancellationToken)
    {
        if (!await _groups.AnyAsync(x => x.Id == input.WorkFlowDef.GroupId && x.Status == 1, cancellationToken))
            throw new BusinessException("流程分组不存在或已停用");
        if (string.IsNullOrWhiteSpace(input.WorkFlowDef.Name)) throw new BusinessException("流程名称不能为空");

        var userId = GetCurrentUserId();
        var defId = _ids.Create();
        var admins = input.WorkFlowDef.FlowAdminIds.Where(IsGuid).Distinct().ToList();
        if (admins.Count == 0) admins.Add(userId.ToString());
        var definition = new OaProcessDefinition(defId)
        {
            PermissionType = (OaPermissionType)input.FlowPermission.Type,
            BelongKey = previous?.BelongKey ?? defId.ToString(),
            Version = previous?.Version + 1 ?? 1,
            Name = input.WorkFlowDef.Name.Trim(),
            Icon = input.WorkFlowDef.Icon,
            GroupId = input.WorkFlowDef.GroupId,
            Cancelable = input.WorkFlowDef.Cancelable == 1,
            FlowAdminIds = admins,
            Status = OaDefinitionStatus.Published,
            DefJson = string.IsNullOrWhiteSpace(input.FlowDefJson) ? JsonSerializer.Serialize(input) : input.FlowDefJson
        };
        await _definitions.InsertAsync(definition, autoSave: true, cancellationToken: cancellationToken);

        var fields = input.FlowWidgets.Select((field, index) => new OaFormField(_ids.Create())
        {
            DefId = defId, FieldKey = field.Name, FieldType = field.Type,
            Label = field.Label ?? string.Empty, IsSummary = field.Summary, IsRequired = field.Required,
            Placeholder = field.Placeholder, SortOrder = index + 1,
            Extras = field.Details == null ? null : JsonSerializer.Serialize(field.Details)
        }).ToList();
        if (fields.Count > 0) await _fields.InsertManyAsync(fields, autoSave: true, cancellationToken: cancellationToken);

        var initiators = (input.FlowPermission.FlowInitiators ?? []).GroupBy(x => x.Type).Select(group => new OaInitiator(_ids.Create())
        {
            DefId = defId, InitiatorType = group.Key,
            InitiatorIds = group.Select(x => x.Id).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList()
        }).ToList();
        if (initiators.Count > 0) await _initiators.InsertManyAsync(initiators, autoSave: true, cancellationToken: cancellationToken);

        var flattened = Flatten(defId, input.NodeConfig);
        await _nodes.InsertManyAsync(flattened.Nodes, autoSave: true, cancellationToken: cancellationToken);
        if (flattened.Groups.Count > 0) await _conditionGroups.InsertManyAsync(flattened.Groups, autoSave: true, cancellationToken: cancellationToken);
        if (flattened.Conditions.Count > 0) await _conditions.InsertManyAsync(flattened.Conditions, autoSave: true, cancellationToken: cancellationToken);
        if (flattened.Approvers.Count > 0) await _approvers.InsertManyAsync(flattened.Approvers, autoSave: true, cancellationToken: cancellationToken);
        if (flattened.Ccs.Count > 0) await _ccConfigs.InsertManyAsync(flattened.Ccs, autoSave: true, cancellationToken: cancellationToken);
        return definition;
    }

    public async Task DeleteProcessAsync(Guid defId, CancellationToken cancellationToken = default)
    {
        var definition = await _definitions.GetAsync(defId, cancellationToken: cancellationToken);
        EnsureFlowAdmin(definition);
        var versions = await (await _definitions.GetQueryableAsync()).Where(x => x.BelongKey == definition.BelongKey).ToListAsync(cancellationToken);
        var versionIds = versions.Select(x => x.Id).ToList();
        if (await _instances.AnyAsync(x => versionIds.Contains(x.DefId), cancellationToken))
            throw new BusinessException("该流程已有实例，不能删除，可改为停用");
        foreach (var version in versions) await DeleteDefinitionGraphAsync(version, cancellationToken);
    }

    public async Task SetProcessEnabledAsync(Guid defId, bool enabled, CancellationToken cancellationToken = default)
    {
        var definition = await _definitions.GetAsync(defId, cancellationToken: cancellationToken);
        EnsureFlowAdmin(definition);
        if (enabled)
        {
            var published = await (await _definitions.GetQueryableAsync())
                .Where(x => x.BelongKey == definition.BelongKey && x.Id != definition.Id && x.Status == OaDefinitionStatus.Published)
                .ToListAsync(cancellationToken);
            foreach (var item in published) item.Status = OaDefinitionStatus.Disabled;
            if (published.Count > 0) await _definitions.UpdateManyAsync(published, autoSave: true, cancellationToken: cancellationToken);
        }
        definition.Status = enabled ? OaDefinitionStatus.Published : OaDefinitionStatus.Disabled;
        await _definitions.UpdateAsync(definition, autoSave: true, cancellationToken: cancellationToken);
    }

    public async Task<OaFlowDefinitionDto> CopyProcessAsync(OaCopyProcessRequest input, CancellationToken cancellationToken = default)
    {
        var source = await _definitions.GetAsync(input.FlowDefId, cancellationToken: cancellationToken);
        EnsureFlowAdmin(source);
        var name = input.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessException("流程名称不能为空");

        var json = JsonNode.Parse(source.DefJson)?.AsObject() ?? throw new BusinessException("流程设计数据无效");
        var workflowNode = json["workFlowDef"] ?? json["WorkFlowDef"];
        if (workflowNode is not JsonObject workflow) throw new BusinessException("流程设计数据无效");
        workflow["id"] = null;
        workflow["name"] = name;
        var copiedJson = json.ToJsonString();
        var model = JsonSerializer.Deserialize<OaAddProcessRequest>(copiedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new BusinessException("流程设计数据无效");
        model.WorkFlowDef.Id = null;
        model.WorkFlowDef.Name = name;
        model.FlowDefJson = copiedJson;
        var copied = await SaveProcessAsync(model, null, cancellationToken);
        return new OaFlowDefinitionDto
        {
            Id = copied.Id,
            LinkId = copied.BelongKey,
            Name = copied.Name,
            Icon = copied.Icon,
            InitiatorType = (int)copied.PermissionType,
            Version = copied.Version,
            GroupId = copied.GroupId,
            Cancelable = copied.Cancelable,
            Status = 0,
            Editable = true,
            FlowInitiators = model.FlowPermission.FlowInitiators ?? []
        };
    }

    private async Task DeleteDefinitionGraphAsync(OaProcessDefinition definition, CancellationToken cancellationToken)
    {
        var nodes = await (await _nodes.GetQueryableAsync()).Where(x => x.DefId == definition.Id).ToListAsync(cancellationToken);
        var nodeIds = nodes.Select(x => x.Id).ToList();
        var groups = await (await _conditionGroups.GetQueryableAsync()).Where(x => nodeIds.Contains(x.NodeId)).ToListAsync(cancellationToken);
        var groupIds = groups.Select(x => x.Id).ToList();
        var conditions = await (await _conditions.GetQueryableAsync()).Where(x => groupIds.Contains(x.GroupId)).ToListAsync(cancellationToken);
        var approvers = await (await _approvers.GetQueryableAsync()).Where(x => nodeIds.Contains(x.NodeId)).ToListAsync(cancellationToken);
        var ccs = await (await _ccConfigs.GetQueryableAsync()).Where(x => nodeIds.Contains(x.NodeId)).ToListAsync(cancellationToken);
        var fields = await (await _fields.GetQueryableAsync()).Where(x => x.DefId == definition.Id).ToListAsync(cancellationToken);
        var initiators = await (await _initiators.GetQueryableAsync()).Where(x => x.DefId == definition.Id).ToListAsync(cancellationToken);
        if (conditions.Count > 0) await _conditions.DeleteManyAsync(conditions, autoSave: true, cancellationToken: cancellationToken);
        if (groups.Count > 0) await _conditionGroups.DeleteManyAsync(groups, autoSave: true, cancellationToken: cancellationToken);
        if (approvers.Count > 0) await _approvers.DeleteManyAsync(approvers, autoSave: true, cancellationToken: cancellationToken);
        if (ccs.Count > 0) await _ccConfigs.DeleteManyAsync(ccs, autoSave: true, cancellationToken: cancellationToken);
        if (nodes.Count > 0) await _nodes.DeleteManyAsync(nodes, autoSave: true, cancellationToken: cancellationToken);
        if (fields.Count > 0) await _fields.DeleteManyAsync(fields, autoSave: true, cancellationToken: cancellationToken);
        if (initiators.Count > 0) await _initiators.DeleteManyAsync(initiators, autoSave: true, cancellationToken: cancellationToken);
        await _definitions.DeleteAsync(definition, autoSave: true, cancellationToken: cancellationToken);
    }

    public async Task<OaProcessEditDto> GetProcessEditDataAsync(Guid defId, CancellationToken cancellationToken = default)
    {
        var definition = await _definitions.GetAsync(defId, cancellationToken: cancellationToken);
        EnsureFlowAdmin(definition);
        return new OaProcessEditDto { FlowDefId = definition.Id, FlowDefJson = definition.DefJson };
    }

    public Task<PagedResultDto<OaFlowInstanceListDto>> QueryInstancesAsync(OaFlowInstanceQuery input, CancellationToken cancellationToken = default) =>
        QueryInstancePageAsync(input, null, cancellationToken);

    public async Task<List<OaFlowFormFieldDto>> GetFlowFormWidgetsAsync(Guid defId, CancellationToken cancellationToken = default) =>
        await (await _fields.GetQueryableAsync()).AsNoTracking().Where(x => x.DefId == defId).OrderBy(x => x.SortOrder)
            .Select(x => new OaFlowFormFieldDto
            {
                Id = x.Id, FlowDefId = x.DefId, Name = x.FieldKey, Label = x.Label, Placeholder = x.Placeholder,
                Type = x.FieldType, Required = x.IsRequired, Summary = x.IsSummary
            }).ToListAsync(cancellationToken);

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
            instanceQuery = instanceQuery.Where(x => defIds.Contains(x.DefId));
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
                FlowDefId = definition.Id, Name = definition.Name, GroupId = definition.GroupId, Cancelable = definition.Cancelable,
                Id = instance.Id, InitiatorId = instance.Initiator.ToString(), BeginTime = instance.CreationTime, EndTime = instance.EndTime,
                Status = (int)instance.Status, TaskId = relevantTask?.Id, ActNodeId = relevantTask?.NodeId,
                Assignable = node?.Assignable ?? false, Signable = node?.Signable ?? false, Backable = node?.Backable ?? false,
                Signature = node?.Signature ?? false, NodeType = node == null ? 0 : (int)node.NodeType,
                Summary = BuildSummary(instance.FormValue)
            };
        }).ToList();
        return new PagedResultDto<OaFlowInstanceListDto> { Total = total, Items = items };
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

    private FlattenResult Flatten(Guid defId, OaFlowNodeRequest root)
    {
        var result = new FlattenResult();
        BuildNode(root, defId, null, result);
        var end = result.Nodes.FirstOrDefault(x => x.NodeType == OaNodeType.End) ?? new OaNode(_ids.Create())
        {
            DefId = defId, NodeName = "结束", NodeType = OaNodeType.End
        };
        if (!result.Nodes.Contains(end)) result.Nodes.Add(end);
        foreach (var leaf in result.Nodes.Where(x => x.Id != end.Id && x.ChildNodeId == null && x.NodeType != OaNodeType.End)) leaf.ChildNodeId = end.Id;
        return result;
    }

    private Guid BuildNode(OaFlowNodeRequest source, Guid defId, Guid? parentId, FlattenResult result)
    {
        var node = new OaNode(_ids.Create())
        {
            DefId = defId, NodeName = source.Name, NodeType = (OaNodeType)source.Type,
            ParentNodeId = parentId, IsConditionBranch = source.Type == (int)OaNodeType.Condition,
            PriorityLevel = source.PriorityLevel, ConditionExpression = source.ConditionExpression,
            ApprovalType = source.ApprovalType, MultiInstanceApprovalType = source.MultiInstanceApprovalType,
            FlowNodeNoAuditorType = source.FlowNodeNoAuditorType, FlowNodeNoAuditorAssignee = source.FlowNodeNoAuditorAssignee,
            FlowNodeSelfAuditorType = source.FlowNodeSelfAuditorType, Backable = source.Backable,
            Signable = source.Signable, Assignable = source.Assignable, Signature = source.Signature
        };
        result.Nodes.Add(node);
        foreach (var assignee in source.Assignees ?? [])
            result.Approvers.Add(new OaApproverConfig(_ids.Create())
            {
                NodeId = node.Id, Rid = assignee.Rid, AssigneeType = (OaAssigneeType)assignee.AssigneeType,
                Assignees = assignee.Assignees ?? [], Roles = assignee.Roles ?? [], Layer = assignee.Layer, LayerType = assignee.LayerType
            });
        foreach (var cc in source.Ccs ?? [])
            result.Ccs.Add(new OaCcConfig(_ids.Create())
            {
                NodeId = node.Id, Rid = cc.Rid, CcType = cc.CcType,
                Assignees = cc.Assignees ?? [], Roles = cc.Roles ?? [], Layer = cc.Layer, LayerType = cc.LayerType
            });
        foreach (var branchSource in source.ConditionNodes ?? [])
        {
            var branchId = BuildNode(branchSource, defId, node.Id, result);
            var branch = result.Nodes.First(x => x.Id == branchId);
            branch.IsConditionBranch = true;
            foreach (var groupSource in branchSource.ConditionGroups ?? [])
            {
                var group = new OaConditionGroup(_ids.Create()) { NodeId = branch.Id, GroupKey = groupSource.Id };
                result.Groups.Add(group);
                foreach (var condition in groupSource.Conditions ?? [])
                    result.Conditions.Add(new OaCondition(_ids.Create())
                    {
                        GroupId = group.Id, VarName = condition.VarName, Operator = condition.Operator,
                        Values = condition.Val, Operators = condition.Operators
                    });
            }
        }
        if (source.ChildNode != null) node.ChildNodeId = BuildNode(source.ChildNode, defId, node.Id, result);
        return node.Id;
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

    private void EnsureFlowAdmin(OaProcessDefinition definition)
    {
        var userId = GetCurrentUserId().ToString();
        if (definition.FlowAdminIds.Count > 0 && !definition.FlowAdminIds.Contains(userId)) throw new BusinessException("当前用户不是流程管理员");
    }

    private Guid GetCurrentUserId() => _currentUser.Id is { } id && id != Guid.Empty ? id : throw new BusinessException("未登录或用户信息无效");
    private static bool IsGuid(string value) => Guid.TryParse(value, out _);
    private static Guid ParseUserId(string? value, string message) => Guid.TryParse(value, out var id) && id != Guid.Empty ? id : throw new BusinessException(message);
    private static void AddGuids(HashSet<Guid> target, IEnumerable<string> source) { foreach (var value in source) if (Guid.TryParse(value, out var id)) target.Add(id); }
    private static string JsonValue(JsonElement value) => value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString();
    private static void EnsureJsonObject(string value) { try { using var document = JsonDocument.Parse(value); if (document.RootElement.ValueKind != JsonValueKind.Object) throw new JsonException(); } catch (JsonException) { throw new BusinessException("流程表单数据格式无效"); } }
    private static string? BuildSummary(string formValue) { try { using var doc = JsonDocument.Parse(formValue); return string.Join("，", doc.RootElement.EnumerateObject().Take(3).Select(x => JsonValue(x.Value))).Truncate(160); } catch { return null; } }

    private enum QueryScope { Pending, Mine, Cc, Audited }
    private sealed class FlattenResult
    {
        public List<OaNode> Nodes { get; } = [];
        public List<OaConditionGroup> Groups { get; } = [];
        public List<OaCondition> Conditions { get; } = [];
        public List<OaApproverConfig> Approvers { get; } = [];
        public List<OaCcConfig> Ccs { get; } = [];
    }
}
