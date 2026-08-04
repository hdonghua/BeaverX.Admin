using System.Text.Json;
using System.Text.Json.Nodes;
using BeaverX.Admin.Application.Contracts.Oa;
using BeaverX.Admin.Domain.Oa;
using BeaverX.Admin.Domain.Shared.Oa;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Oa;

public partial class OaWorkflowAppService
{
    private NodeRuntimeOptions GetNodeRuntimeOptions(OaNode node)
    {
        if (string.IsNullOrWhiteSpace(node.Extras)) return new NodeRuntimeOptions();
        try
        {
            return JsonSerializer.Deserialize<NodeRuntimeOptions>(node.Extras, WorkflowJsonOptions) ?? new NodeRuntimeOptions();
        }
        catch (JsonException)
        {
            return new NodeRuntimeOptions();
        }
    }

    private void ApplyNodeFormAuth(List<OaFlowFormFieldDto> fields, OaNode node)
    {
        var auths = GetNodeRuntimeOptions(node).FormAuths;
        if (auths.Count == 0) return;
        var authMap = auths.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            if (!authMap.TryGetValue(field.Name, out var auth))
            {
                field.Readable = true;
                field.Editable = false;
                continue;
            }

            field.Readable = auth.Readable ?? true;
            field.Editable = auth.Editable ?? false;
            if (auth.Details is not { Count: > 0 } || field.ExtraProperties?.TryGetValue("details", out var detailsElement) != true)
                continue;

            var details = JsonNode.Parse(detailsElement.GetRawText()) as JsonArray;
            if (details == null) continue;
            var detailAuth = auth.Details.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
            foreach (var detail in details.OfType<JsonObject>())
            {
                var name = detail["name"]?.GetValue<string>();
                if (name != null && detailAuth.TryGetValue(name, out var item))
                {
                    detail["readable"] = item.Readable ?? true;
                    detail["editable"] = item.Editable ?? false;
                }
                else
                {
                    detail["readable"] = true;
                    detail["editable"] = false;
                }
            }
            field.ExtraProperties["details"] = JsonSerializer.SerializeToElement(details, WorkflowJsonOptions);
        }
    }

    private static JsonObject ParseFormObject(string value)
    {
        try
        {
            return JsonNode.Parse(value) as JsonObject ?? throw new JsonException();
        }
        catch (JsonException)
        {
            throw new BusinessException("流程表单数据格式无效");
        }
    }

    private static void ValidateFormValues(JsonObject form, IReadOnlyCollection<OaFlowFormFieldDto> fields)
    {
        foreach (var field in fields)
        {
            form.TryGetPropertyValue(field.Name, out var value);
            if (field.Required && IsEmptyValue(value)) throw new BusinessException($"{field.Label}不能为空");
            if (IsEmptyValue(value)) continue;

            if (field.Type is 3 or 4 or 23 && !decimal.TryParse(value!.ToJsonString().Trim('"'), out _))
                throw new BusinessException($"{field.Label}必须是数字");
            if (field.Type is 6 or 8 or 9 or 10 or 11 or 15 or 16 && value is not JsonArray)
                throw new BusinessException($"{field.Label}的数据格式无效");
            if (field.Type == 9) ValidateDetailValues(field, value as JsonArray);
        }
    }

    private static void ValidateDetailValues(OaFlowFormFieldDto field, JsonArray? rows)
    {
        if (rows == null || field.ExtraProperties?.TryGetValue("details", out var detailsElement) != true) return;
        var details = JsonSerializer.Deserialize<List<OaFlowFormFieldDto>>(detailsElement.GetRawText(), WorkflowJsonOptions) ?? [];
        foreach (var row in rows.OfType<JsonObject>())
            foreach (var detail in details.Where(x => x.Required))
                if (!row.TryGetPropertyValue(detail.Name, out var value) || IsEmptyValue(value))
                    throw new BusinessException($"{field.Label}中的{detail.Label}不能为空");
    }

    private static bool IsEmptyValue(JsonNode? value) => value switch
    {
        null => true,
        JsonArray array => array.Count == 0,
        JsonObject obj => obj.Count == 0,
        JsonValue scalar => string.IsNullOrWhiteSpace(scalar.ToJsonString().Trim('"')) || scalar.ToJsonString() == "null",
        _ => false
    };

    private void EnsureOnlyEditableValuesChanged(string oldValue, string newValue, OaNode node)
    {
        var oldForm = ParseFormObject(oldValue);
        var newForm = ParseFormObject(newValue);
        var authMap = GetNodeRuntimeOptions(node).FormAuths.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        var names = oldForm.Select(x => x.Key).Union(newForm.Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            oldForm.TryGetPropertyValue(name, out var oldField);
            newForm.TryGetPropertyValue(name, out var newField);
            if (JsonNode.DeepEquals(oldField, newField)) continue;
            if (!authMap.TryGetValue(name, out var auth)) throw new BusinessException($"字段 {name} 不允许修改");
            if (auth.Editable == true) continue;
            if (auth.Details is not { Count: > 0 } || oldField is not JsonArray oldRows || newField is not JsonArray newRows)
                throw new BusinessException($"字段 {name} 不允许修改");
            EnsureOnlyEditableDetailValuesChanged(name, oldRows, newRows, auth.Details);
        }
    }

    private static void EnsureOnlyEditableDetailValuesChanged(string fieldName, JsonArray oldRows, JsonArray newRows, List<OaFormAuthRequest> auths)
    {
        var editable = auths.Where(x => x.Editable == true).Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (editable.Count == 0) throw new BusinessException($"字段 {fieldName} 不允许修改");
        for (var index = 0; index < Math.Min(oldRows.Count, newRows.Count); index++)
        {
            if (oldRows[index] is not JsonObject oldRow || newRows[index] is not JsonObject newRow) continue;
            var names = oldRow.Select(x => x.Key).Union(newRow.Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
            foreach (var name in names)
            {
                oldRow.TryGetPropertyValue(name, out var oldCell);
                newRow.TryGetPropertyValue(name, out var newCell);
                if (!editable.Contains(name) && !JsonNode.DeepEquals(oldCell, newCell))
                    throw new BusinessException($"字段 {fieldName}.{name} 不允许修改");
            }
        }
        foreach (var row in newRows.Skip(oldRows.Count).OfType<JsonObject>())
            if (row.Any(x => !editable.Contains(x.Key) && !IsEmptyValue(x.Value)))
                throw new BusinessException($"字段 {fieldName} 包含不可编辑的数据");
    }

    private async Task<List<Guid>> ResolveConfiguredUsersAsync(
        Guid initiator,
        OaAssigneeType assigneeType,
        IReadOnlyCollection<string> assignees,
        IReadOnlyCollection<string> roles,
        int? layerType,
        int? layer,
        CancellationToken cancellationToken)
    {
        if (assigneeType == OaAssigneeType.Self) return [initiator];
        if (assigneeType == OaAssigneeType.Assignee)
            return assignees.Where(IsGuid).Select(Guid.Parse).Distinct().ToList();
        if (assigneeType == OaAssigneeType.Role)
        {
            var roleIds = (roles.Count > 0 ? roles : assignees).Where(IsGuid).Select(Guid.Parse).Distinct().ToList();
            return roleIds.Count == 0
                ? []
                : await (await _userRoles.GetQueryableAsync()).AsNoTracking().Where(x => roleIds.Contains(x.RoleId))
                    .OrderBy(x => x.Id).Select(x => x.UserId).Distinct().ToListAsync(cancellationToken);
        }
        if (assigneeType is OaAssigneeType.Superior or OaAssigneeType.MultistepLeader)
        {
            var multiple = assigneeType == OaAssigneeType.MultistepLeader;
            return await ResolveManagerChainAsync(initiator, layerType ?? 0, layer ?? 0, multiple, cancellationToken);
        }
        if (assigneeType is OaAssigneeType.DepartmentLeader or OaAssigneeType.MultistepDepartmentLeader)
        {
            var multiple = assigneeType == OaAssigneeType.MultistepDepartmentLeader;
            return await ResolveDepartmentLeaderChainAsync(initiator, layerType ?? 0, layer ?? 0, multiple, cancellationToken);
        }
        return [];
    }

    private async Task<List<Guid>> ResolveManagerChainAsync(Guid userId, int layerType, int layer, bool multiple, CancellationToken cancellationToken)
    {
        var links = await (await _userDepartments.GetQueryableAsync()).AsNoTracking().Where(x => x.IsPrimary)
            .Select(x => new { x.UserId, x.ManagerUserId }).ToListAsync(cancellationToken);
        var managerMap = links.ToDictionary(x => x.UserId, x => x.ManagerUserId);
        var chain = new List<Guid>();
        var visited = new HashSet<Guid> { userId };
        var current = managerMap.GetValueOrDefault(userId);
        while (current.HasValue && visited.Add(current.Value))
        {
            chain.Add(current.Value);
            current = managerMap.GetValueOrDefault(current.Value);
        }
        return SelectHierarchyUsers(chain, layerType, layer, multiple);
    }

    private async Task<List<Guid>> ResolveDepartmentLeaderChainAsync(Guid userId, int layerType, int layer, bool multiple, CancellationToken cancellationToken)
    {
        var memberships = await (await _userDepartments.GetQueryableAsync()).AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        if (memberships.Count == 0) return [];
        var departments = await (await _departments.GetQueryableAsync()).AsNoTracking().Where(x => x.IsEnabled).ToListAsync(cancellationToken);
        var map = departments.ToDictionary(x => x.Id);
        var chain = new List<OaDepartment>();
        var visited = new HashSet<Guid>();
        var currentId = memberships[0].DepartmentId;
        while (map.TryGetValue(currentId, out var department) && visited.Add(currentId))
        {
            chain.Add(department);
            if (!department.ParentId.HasValue) break;
            currentId = department.ParentId.Value;
        }
        if (chain.Count == 0) return [];

        var targetIndex = GetHierarchyTargetIndex(chain.Count, layerType, layer);
        var selected = multiple ? chain.Take(targetIndex + 1) : chain.Skip(targetIndex).Take(1);
        return selected.Where(x => x.LeaderUserId.HasValue).Select(x => x.LeaderUserId!.Value).Distinct().ToList();
    }

    private static List<Guid> SelectHierarchyUsers(IReadOnlyList<Guid> chain, int layerType, int layer, bool multiple)
    {
        if (chain.Count == 0) return [];
        var targetIndex = GetHierarchyTargetIndex(chain.Count, layerType, layer);
        return (multiple ? chain.Take(targetIndex + 1) : chain.Skip(targetIndex).Take(1)).Distinct().ToList();
    }

    private static int GetHierarchyTargetIndex(int count, int layerType, int layer) => layerType == 0
        ? Math.Min(Math.Max(layer, 0), count - 1)
        : Math.Max(count - 1 - Math.Max(layer, 0), 0);

    private async Task<HashSet<string>> GetInitiatorIdentitiesAsync(Guid initiator, CancellationToken cancellationToken)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { initiator.ToString() };
        var roleIds = await (await _userRoles.GetQueryableAsync()).AsNoTracking().Where(x => x.UserId == initiator)
            .Select(x => x.RoleId).ToListAsync(cancellationToken);
        foreach (var roleId in roleIds) result.Add(roleId.ToString());
        var memberships = await (await _userDepartments.GetQueryableAsync()).AsNoTracking().Where(x => x.UserId == initiator)
            .Select(x => x.DepartmentId).ToListAsync(cancellationToken);
        var departments = await (await _departments.GetQueryableAsync()).AsNoTracking().ToListAsync(cancellationToken);
        var parentMap = departments.ToDictionary(x => x.Id, x => x.ParentId);
        foreach (var membership in memberships)
        {
            var current = (Guid?)membership;
            var visited = new HashSet<Guid>();
            while (current.HasValue && visited.Add(current.Value))
            {
                result.Add(current.Value.ToString());
                current = parentMap.GetValueOrDefault(current.Value);
            }
        }
        return result;
    }
}
