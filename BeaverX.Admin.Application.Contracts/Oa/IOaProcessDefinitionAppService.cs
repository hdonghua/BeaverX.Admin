namespace BeaverX.Admin.Application.Contracts.Oa;

public interface IOaProcessDefinitionAppService
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
    Task<List<OaFlowFormFieldDto>> GetFlowFormWidgetsAsync(Guid defId, CancellationToken cancellationToken = default);
}
