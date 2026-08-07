using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
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
        ValidateFieldKeys(input.FlowWidgets);
        ValidateServiceTaskHandlers(input.NodeConfig);

        var userId = GetCurrentUserId();
        var defId = _ids.Create();
        var belongKey = await ResolveBelongKeyAsync(input.WorkFlowDef.ProcessKey, previous, defId, cancellationToken);
        var admins = input.WorkFlowDef.FlowAdminIds.Where(IsGuid).Distinct().ToList();
        if (admins.Count == 0) admins.Add(userId.ToString());
        var definition = new OaProcessDefinition(defId)
        {
            PermissionType = (OaPermissionType)input.FlowPermission.Type,
            BelongKey = belongKey,
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
            DefId = defId,
            FieldKey = field.Name,
            FieldType = field.Type,
            Label = field.Label ?? string.Empty,
            IsSummary = field.Summary,
            IsRequired = field.Required,
            Placeholder = field.Placeholder,
            SortOrder = index + 1,
            Extras = JsonSerializer.Serialize(field, WorkflowJsonOptions)
        }).ToList();
        if (fields.Count > 0) await _fields.InsertManyAsync(fields, autoSave: true, cancellationToken: cancellationToken);

        var initiators = (input.FlowPermission.FlowInitiators ?? []).GroupBy(x => x.Type).Select(group => new OaInitiator(_ids.Create())
        {
            DefId = defId,
            InitiatorType = group.Key,
            InitiatorIds = group.Select(x => x.Id).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList()
        }).ToList();
        if (initiators.Count > 0) await _initiators.InsertManyAsync(initiators, autoSave: true, cancellationToken: cancellationToken);

        var flattened = Flatten(defId, input.NodeConfig);
        await _nodes.InsertManyAsync(flattened.Nodes, autoSave: true, cancellationToken: cancellationToken);
        if (flattened.Groups.Count > 0) await _conditionGroups.InsertManyAsync(flattened.Groups, autoSave: true, cancellationToken: cancellationToken);
        if (flattened.Conditions.Count > 0) await _conditions.InsertManyAsync(flattened.Conditions, autoSave: true, cancellationToken: cancellationToken);
        if (flattened.Approvers.Count > 0) await _approvers.InsertManyAsync(flattened.Approvers, autoSave: true, cancellationToken: cancellationToken);
        if (flattened.Ccs.Count > 0) await _ccConfigs.InsertManyAsync(flattened.Ccs, autoSave: true, cancellationToken: cancellationToken);
        if (flattened.Transactors.Count > 0) await _transactConfigs.InsertManyAsync(flattened.Transactors, autoSave: true, cancellationToken: cancellationToken);
        return definition;
    }

    public Task<List<OaServiceTaskHandlerDto>> GetServiceTaskHandlersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_serviceTaskHandlers.Values
            .OrderBy(x => x.DisplayName)
            .Select(x => new OaServiceTaskHandlerDto { Key = x.Key, Name = x.DisplayName })
            .ToList());

    public Task<List<OaWorkflowKeyOptionDto>> GetWorkflowKeyOptionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(OaWorkflowKeys.Options
            .Select(x => new OaWorkflowKeyOptionDto { Key = x.Key, Name = x.Value })
            .ToList());

    private async Task<string> ResolveBelongKeyAsync(
        string? requestedKey,
        OaProcessDefinition? previous,
        Guid defId,
        CancellationToken cancellationToken)
    {
        var key = requestedKey?.Trim();
        if (string.IsNullOrWhiteSpace(key)) return previous?.BelongKey ?? defId.ToString();
        if (key.Length > 64) throw new BusinessException("流程 Key 不能超过 64 个字符");
        if (!Regex.IsMatch(key, "^[A-Za-z][A-Za-z0-9_.-]*$"))
            throw new BusinessException("流程 Key 必须以字母开头，且只能包含字母、数字、下划线、点和横线");

        var normalized = key.ToLower();
        var matches = await (await _definitions.GetQueryableAsync())
            .Where(x => x.BelongKey.ToLower() == normalized)
            .ToListAsync(cancellationToken);
        if (matches.Any(x => previous == null || !string.Equals(x.BelongKey, previous.BelongKey, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessException($"流程 Key“{key}”已被其他流程使用");

        if (previous != null && !string.Equals(previous.BelongKey, key, StringComparison.Ordinal))
        {
            var versions = await (await _definitions.GetQueryableAsync())
                .Where(x => x.BelongKey == previous.BelongKey)
                .ToListAsync(cancellationToken);
            foreach (var version in versions) version.BelongKey = key;
            if (versions.Count > 0)
                await _definitions.UpdateManyAsync(versions, autoSave: true, cancellationToken: cancellationToken);
        }
        return key;
    }

    private void ValidateServiceTaskHandlers(OaFlowNodeRequest node)
    {
        if (node.Type == (int)OaNodeType.ServiceTask)
        {
            var keys = (node.ServiceTaskHandlers ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (keys.Count == 0) throw new BusinessException($"服务任务节点“{node.Name}”至少需要选择一个处理器");
            var missing = keys.Where(x => !_serviceTaskHandlers.ContainsKey(x)).ToList();
            if (missing.Count > 0) throw new BusinessException($"服务任务节点“{node.Name}”包含不可用的处理器：{string.Join("、", missing)}");
        }

        if (node.ChildNode != null) ValidateServiceTaskHandlers(node.ChildNode);
        foreach (var branch in node.ConditionNodes ?? []) ValidateServiceTaskHandlers(branch);
    }

    private static void ValidateFieldKeys(IEnumerable<OaFlowWidgetRequest> widgets)
    {
        ValidateFieldKeys(widgets, new HashSet<string>(StringComparer.Ordinal));
    }

    private static void ValidateFieldKeys(
        IEnumerable<OaFlowWidgetRequest> widgets,
        HashSet<string> keys)
    {
        foreach (var widget in widgets ?? [])
        {
            ValidateFieldKey(widget.Name, widget.Label, keys);

            if (widget.Details is null) continue;
            var detailsJson = JsonSerializer.Serialize(widget.Details, WorkflowJsonOptions);
            var details = JsonSerializer.Deserialize<List<OaFlowWidgetRequest>>(
                detailsJson,
                WorkflowJsonOptions);
            if (details is not null) ValidateFieldKeys(details, keys);
        }
    }

    private static void ValidateFieldKey(
        string? fieldKey,
        string? label,
        HashSet<string> keys)
    {
        if (string.IsNullOrWhiteSpace(fieldKey) ||
            !Regex.IsMatch(fieldKey, "^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.CultureInvariant))
            throw new BusinessException($"表单控件“{label ?? string.Empty}”的字段标识格式不正确");

        if (!keys.Add(fieldKey))
            throw new BusinessException($"表单字段标识不能重复：{fieldKey}");
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
        workflow["processKey"] = null;
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
        var transactors = await (await _transactConfigs.GetQueryableAsync()).Where(x => nodeIds.Contains(x.NodeId)).ToListAsync(cancellationToken);
        var fields = await (await _fields.GetQueryableAsync()).Where(x => x.DefId == definition.Id).ToListAsync(cancellationToken);
        var initiators = await (await _initiators.GetQueryableAsync()).Where(x => x.DefId == definition.Id).ToListAsync(cancellationToken);
        if (conditions.Count > 0) await _conditions.DeleteManyAsync(conditions, autoSave: true, cancellationToken: cancellationToken);
        if (groups.Count > 0) await _conditionGroups.DeleteManyAsync(groups, autoSave: true, cancellationToken: cancellationToken);
        if (approvers.Count > 0) await _approvers.DeleteManyAsync(approvers, autoSave: true, cancellationToken: cancellationToken);
        if (ccs.Count > 0) await _ccConfigs.DeleteManyAsync(ccs, autoSave: true, cancellationToken: cancellationToken);
        if (transactors.Count > 0) await _transactConfigs.DeleteManyAsync(transactors, autoSave: true, cancellationToken: cancellationToken);
        if (nodes.Count > 0) await _nodes.DeleteManyAsync(nodes, autoSave: true, cancellationToken: cancellationToken);
        if (fields.Count > 0) await _fields.DeleteManyAsync(fields, autoSave: true, cancellationToken: cancellationToken);
        if (initiators.Count > 0) await _initiators.DeleteManyAsync(initiators, autoSave: true, cancellationToken: cancellationToken);
        await _definitions.DeleteAsync(definition, autoSave: true, cancellationToken: cancellationToken);
    }

    public async Task<OaProcessEditDto> GetProcessEditDataAsync(Guid defId, CancellationToken cancellationToken = default)
    {
        var definition = await _definitions.GetAsync(defId, cancellationToken: cancellationToken);
        EnsureFlowAdmin(definition);
        var json = JsonNode.Parse(definition.DefJson)?.AsObject() ?? throw new BusinessException("流程设计数据无效");
        var workflow = json["workFlowDef"] ?? json["WorkFlowDef"];
        if (workflow is JsonObject workflowObject) workflowObject["processKey"] = definition.BelongKey;
        return new OaProcessEditDto { FlowDefId = definition.Id, FlowDefJson = json.ToJsonString() };
    }

    public async Task<List<OaFlowFormFieldDto>> GetFlowFormWidgetsAsync(Guid defId, CancellationToken cancellationToken = default)
    {
        var fields = await (await _fields.GetQueryableAsync()).AsNoTracking().Where(x => x.DefId == defId)
            .OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return fields.Select(MapFormField).ToList();
    }

    private static OaFlowFormFieldDto MapFormField(OaFormField field)
    {
        OaFlowFormFieldDto dto;
        try
        {
            dto = !string.IsNullOrWhiteSpace(field.Extras) && field.Extras.TrimStart().StartsWith('{')
                ? JsonSerializer.Deserialize<OaFlowFormFieldDto>(field.Extras, WorkflowJsonOptions) ?? new OaFlowFormFieldDto()
                : new OaFlowFormFieldDto();
            if (!string.IsNullOrWhiteSpace(field.Extras) && field.Extras.TrimStart().StartsWith('['))
            {
                using var details = JsonDocument.Parse(field.Extras);
                dto.ExtraProperties = new Dictionary<string, JsonElement> { ["details"] = details.RootElement.Clone() };
            }
        }
        catch (JsonException)
        {
            dto = new OaFlowFormFieldDto();
        }

        dto.Id = field.Id;
        dto.FlowDefId = field.DefId;
        dto.Name = field.FieldKey;
        dto.Label = field.Label;
        dto.Placeholder = field.Placeholder;
        dto.Type = field.FieldType;
        dto.Required = field.IsRequired;
        dto.Summary = field.IsSummary;
        return dto;
    }

    private FlattenResult Flatten(Guid defId, OaFlowNodeRequest root)
    {
        var result = new FlattenResult();
        BuildNode(root, defId, null, result);
        var end = result.Nodes.FirstOrDefault(x => x.NodeType == OaNodeType.End) ?? new OaNode(_ids.Create())
        {
            DefId = defId,
            NodeName = "结束",
            NodeType = OaNodeType.End
        };
        if (!result.Nodes.Contains(end)) result.Nodes.Add(end);
        foreach (var leaf in result.Nodes.Where(x => x.Id != end.Id && x.ChildNodeId == null && x.NodeType != OaNodeType.End)) leaf.ChildNodeId = end.Id;
        return result;
    }

    private Guid BuildNode(OaFlowNodeRequest source, Guid defId, Guid? parentId, FlattenResult result)
    {
        var node = new OaNode(_ids.Create())
        {
            DefId = defId,
            NodeName = source.Name,
            NodeType = (OaNodeType)source.Type,
            ParentNodeId = parentId,
            IsConditionBranch = source.Type == (int)OaNodeType.Condition,
            PriorityLevel = source.PriorityLevel,
            ConditionExpression = source.ConditionExpression,
            ApprovalType = source.ApprovalType,
            MultiInstanceApprovalType = source.MultiInstanceApprovalType,
            FlowNodeNoAuditorType = source.FlowNodeNoAuditorType,
            FlowNodeNoAuditorAssignee = source.FlowNodeNoAuditorAssignee,
            FlowNodeSelfAuditorType = source.FlowNodeSelfAuditorType,
            Backable = source.Backable,
            Signable = source.Signable,
            Assignable = source.Assignable,
            Signature = source.Signature,
            Extras = JsonSerializer.Serialize(new NodeRuntimeOptions
            {
                FlowNodeAuditAdmin = source.FlowNodeAuditAdmin,
                FormAuths = source.FormAuths ?? [],
                ServiceTaskHandlers = source.ServiceTaskHandlers ?? []
            }, WorkflowJsonOptions)
        };
        result.Nodes.Add(node);
        foreach (var assignee in source.Assignees ?? [])
        {
            var assigneeType = (OaAssigneeType)assignee.AssigneeType;
            result.Approvers.Add(new OaApproverConfig(_ids.Create())
            {
                NodeId = node.Id,
                Rid = assignee.Rid,
                AssigneeType = assigneeType,
                Assignees = assigneeType == OaAssigneeType.Assignee ? assignee.Assignees ?? [] : [],
                Roles = assigneeType == OaAssigneeType.Role ? assignee.Roles ?? [] : [],
                Layer = IsHierarchyAssigneeType(assigneeType) ? assignee.Layer : null,
                LayerType = IsHierarchyAssigneeType(assigneeType) ? assignee.LayerType : null
            });
        }
        foreach (var cc in source.Ccs ?? [])
        {
            var ccType = (OaAssigneeType)cc.CcType;
            result.Ccs.Add(new OaCcConfig(_ids.Create())
            {
                NodeId = node.Id,
                Rid = cc.Rid,
                CcType = cc.CcType,
                Assignees = ccType == OaAssigneeType.Assignee ? cc.Assignees ?? [] : [],
                Roles = ccType == OaAssigneeType.Role ? cc.Roles ?? [] : [],
                Layer = IsHierarchyAssigneeType(ccType) ? cc.Layer : null,
                LayerType = IsHierarchyAssigneeType(ccType) ? cc.LayerType : null
            });
        }
        foreach (var transactor in source.Transactors ?? [])
        {
            var transactorType = (OaAssigneeType)transactor.TransactorType;
            result.Transactors.Add(new OaTransactConfig(_ids.Create())
            {
                NodeId = node.Id,
                Rid = transactor.Rid,
                AssigneeType = transactor.TransactorType,
                Assignees = transactorType == OaAssigneeType.Assignee ? transactor.Assignees ?? [] : [],
                Roles = transactorType == OaAssigneeType.Role ? transactor.Roles ?? [] : [],
                Layer = IsHierarchyAssigneeType(transactorType) ? transactor.Layer : null,
                LayerType = IsHierarchyAssigneeType(transactorType) ? transactor.LayerType : null
            });
        }
        var branchIds = new List<Guid>();
        foreach (var branchSource in source.ConditionNodes ?? [])
        {
            var branchId = BuildNode(branchSource, defId, node.Id, result);
            branchIds.Add(branchId);
            var branch = result.Nodes.First(x => x.Id == branchId);
            branch.IsConditionBranch = true;
            foreach (var groupSource in branchSource.ConditionGroups ?? [])
            {
                var group = new OaConditionGroup(_ids.Create()) { NodeId = branch.Id, GroupKey = groupSource.Id };
                result.Groups.Add(group);
                foreach (var condition in groupSource.Conditions ?? [])
                    result.Conditions.Add(new OaCondition(_ids.Create())
                    {
                        GroupId = group.Id,
                        VarName = condition.VarName,
                        Operator = condition.Operator,
                        Values = condition.Val,
                        Operators = condition.Operators
                    });
            }
        }
        if (source.ChildNode != null)
        {
            node.ChildNodeId = BuildNode(source.ChildNode, defId, node.Id, result);
            if (node.NodeType == OaNodeType.ExclusiveGateway)
            {
                var nodeMap = result.Nodes.ToDictionary(x => x.Id);
                foreach (var branchId in branchIds)
                    foreach (var leaf in result.Nodes.Where(x => x.ChildNodeId == null && IsDescendantOrSelf(x, branchId, nodeMap)))
                        leaf.ChildNodeId = node.ChildNodeId;
            }
        }
        return node.Id;
    }

    private static bool IsDescendantOrSelf(OaNode node, Guid ancestorId, IReadOnlyDictionary<Guid, OaNode> nodeMap)
    {
        var current = node;
        var visited = new HashSet<Guid>();
        while (visited.Add(current.Id))
        {
            if (current.Id == ancestorId) return true;
            if (!current.ParentNodeId.HasValue || !nodeMap.TryGetValue(current.ParentNodeId.Value, out var parent)) return false;
            current = parent;
        }
        return false;
    }

    private static bool IsHierarchyAssigneeType(OaAssigneeType type) => type is
        OaAssigneeType.Superior or
        OaAssigneeType.DepartmentLeader or
        OaAssigneeType.MultistepLeader or
        OaAssigneeType.MultistepDepartmentLeader;

}
