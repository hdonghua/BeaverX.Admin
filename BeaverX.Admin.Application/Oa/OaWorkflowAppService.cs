using System.Text.Json;
using BeaverX.Admin.Application.Contracts.Oa;
using BeaverX.Admin.Domain.Oa;
using BeaverX.Admin.Domain.Rbac;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Users;

namespace BeaverX.Admin.Application.Oa;

[ExposeServices(
    typeof(IOaProcessDefinitionAppService),
    typeof(IOaWorkflowInstanceAppService),
    typeof(IOaWorkflowDataAppService))]
public partial class OaWorkflowAppService :
    IOaProcessDefinitionAppService,
    IOaWorkflowInstanceAppService,
    IOaWorkflowDataAppService,
    IScopedDependency
{
    private static readonly JsonSerializerOptions WorkflowJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IRepository<OaProcessGroup, Guid> _groups;
    private readonly IRepository<OaProcessDefinition, Guid> _definitions;
    private readonly IRepository<OaFormField, Guid> _fields;
    private readonly IRepository<OaInitiator, Guid> _initiators;
    private readonly IRepository<OaNode, Guid> _nodes;
    private readonly IRepository<OaConditionGroup, Guid> _conditionGroups;
    private readonly IRepository<OaCondition, Guid> _conditions;
    private readonly IRepository<OaApproverConfig, Guid> _approvers;
    private readonly IRepository<OaCcConfig, Guid> _ccConfigs;
    private readonly IRepository<OaTransactConfig, Guid> _transactConfigs;
    private readonly IRepository<OaInstance, Guid> _instances;
    private readonly IRepository<OaTask, Guid> _tasks;
    private readonly IRepository<OaCcRecord, Guid> _ccRecords;
    private readonly IRepository<OaComment, Guid> _comments;
    private readonly IRepository<OaOperationLog, Guid> _logs;
    private readonly IRepository<UserRole, Guid> _userRoles;
    private readonly IRepository<OaDepartment, Guid> _departments;
    private readonly IRepository<OaUserDepartment, Guid> _userDepartments;
    private readonly ICurrentUser _currentUser;
    private readonly IGuidGenerator _ids;

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
        IRepository<OaTransactConfig, Guid> transactConfigs,
        IRepository<OaInstance, Guid> instances,
        IRepository<OaTask, Guid> tasks,
        IRepository<OaCcRecord, Guid> ccRecords,
        IRepository<OaComment, Guid> comments,
        IRepository<OaOperationLog, Guid> logs,
        IRepository<UserRole, Guid> userRoles,
        IRepository<OaDepartment, Guid> departments,
        IRepository<OaUserDepartment, Guid> userDepartments,
        ICurrentUser currentUser,
        IGuidGenerator ids)
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
        _transactConfigs = transactConfigs;
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

    private Guid GetCurrentUserId() => _currentUser.Id is { } id && id != Guid.Empty ? id : throw new BusinessException("未登录或用户信息无效");
    private static bool IsGuid(string value) => Guid.TryParse(value, out _);
    private static Guid ParseUserId(string? value, string message) => Guid.TryParse(value, out var id) && id != Guid.Empty ? id : throw new BusinessException(message);
    private static string JsonValue(JsonElement value) => value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString();
    private static string? BuildSummary(string formValue, IReadOnlyCollection<OaFormField> fields)
    {
        try
        {
            using var doc = JsonDocument.Parse(formValue);
            var values = fields.Where(x => x.IsSummary).OrderBy(x => x.SortOrder)
                .Select(field => doc.RootElement.TryGetProperty(field.FieldKey, out var value)
                    ? $"{field.Label}：{FormatSummaryValue(value)}"
                    : null)
                .Where(x => !string.IsNullOrWhiteSpace(x));
            return string.Join("，", values).Truncate(160);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string FormatSummaryValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Array => string.Join("/", value.EnumerateArray().Select(FormatSummaryValue)),
        JsonValueKind.Object when value.TryGetProperty("name", out var name) => JsonValue(name),
        JsonValueKind.Object when value.TryGetProperty("id", out var id) => JsonValue(id),
        JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
        _ => JsonValue(value)
    };

    private enum QueryScope { Pending, Mine, Cc, Audited }
    private sealed class NodeRuntimeOptions
    {
        public string? FlowNodeAuditAdmin { get; set; }
        public List<OaFormAuthRequest> FormAuths { get; set; } = [];
    }

    private sealed class FlattenResult
    {
        public List<OaNode> Nodes { get; } = [];
        public List<OaConditionGroup> Groups { get; } = [];
        public List<OaCondition> Conditions { get; } = [];
        public List<OaApproverConfig> Approvers { get; } = [];
        public List<OaCcConfig> Ccs { get; } = [];
        public List<OaTransactConfig> Transactors { get; } = [];
    }
}
