using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Oa;

public interface IOaWorkflowAppService
{
    Task<List<OaFlowGroupDto>> GetGroupsWithDefinitionsAsync(OaFlowGroupQuery input, bool onlyEnabled = false, CancellationToken cancellationToken = default);
    Task<OaProcessGroupDto> AddGroupAsync(OaAddProcessGroupRequest input, CancellationToken cancellationToken = default);
    Task<OaProcessGroupDto> UpdateGroupAsync(OaUpdateProcessGroupRequest input, CancellationToken cancellationToken = default);
    Task DeleteGroupAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<OaProcessGroupDto>> GetGroupsAsync(CancellationToken cancellationToken = default);
    Task AddProcessAsync(OaAddProcessRequest input, CancellationToken cancellationToken = default);
    Task UpdateProcessAsync(OaAddProcessRequest input, CancellationToken cancellationToken = default);
    Task DeleteProcessAsync(Guid defId, CancellationToken cancellationToken = default);
    Task SetProcessEnabledAsync(Guid defId, bool enabled, CancellationToken cancellationToken = default);
    Task<OaFlowDefinitionDto> CopyProcessAsync(OaCopyProcessRequest input, CancellationToken cancellationToken = default);
    Task<OaProcessEditDto> GetProcessEditDataAsync(Guid defId, CancellationToken cancellationToken = default);
    Task<PagedResultDto<OaFlowInstanceListDto>> QueryInstancesAsync(OaFlowInstanceQuery input, CancellationToken cancellationToken = default);
    Task<List<OaFlowFormFieldDto>> GetFlowFormWidgetsAsync(Guid defId, CancellationToken cancellationToken = default);
    Task LaunchAsync(OaLaunchRequest input, CancellationToken cancellationToken = default);
    Task<List<OaFlowChartNodeDto>> ViewProcessChartAsync(Guid defId, CancellationToken cancellationToken = default);
    Task<PagedResultDto<OaFlowInstanceListDto>> QueryPendingAsync(OaFlowInstanceQuery input, CancellationToken cancellationToken = default);
    Task<PagedResultDto<OaFlowInstanceListDto>> QueryMyApplyAsync(OaFlowInstanceQuery input, CancellationToken cancellationToken = default);
    Task<PagedResultDto<OaFlowInstanceListDto>> QueryCcAsync(OaFlowInstanceQuery input, CancellationToken cancellationToken = default);
    Task<PagedResultDto<OaFlowInstanceListDto>> QueryAuditedAsync(OaFlowInstanceQuery input, CancellationToken cancellationToken = default);
    Task<OaFlowInstanceDetailsDto> GetDetailsAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task<OaFlowInstanceListDto> GetInstanceSummaryAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task FormModifyAsync(OaFormModifyRequest input, CancellationToken cancellationToken = default);
    Task<bool> HasFormEditRecordAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task<List<OaFormEditRecordDto>> GetFormEditRecordsAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task UrgeAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task CommentAsync(OaCommentRequest input, CancellationToken cancellationToken = default);
    Task ApproveAsync(OaTaskActionRequest input, CancellationToken cancellationToken = default);
    Task AssignAsync(OaTaskActionRequest input, CancellationToken cancellationToken = default);
    Task AddSignAsync(OaTaskActionRequest input, CancellationToken cancellationToken = default);
    Task DelSignAsync(OaTaskActionRequest input, CancellationToken cancellationToken = default);
    Task JumpAsync(OaTaskActionRequest input, CancellationToken cancellationToken = default);
    Task CancelAsync(OaTaskActionRequest input, CancellationToken cancellationToken = default);
    Task TransferAsync(OaTransferRequest input, CancellationToken cancellationToken = default);
}

public interface IOaOrganizationAppService
{
    Task<OaOrganizationOptionsDto> GetOptionsAsync(CancellationToken cancellationToken = default);
}
