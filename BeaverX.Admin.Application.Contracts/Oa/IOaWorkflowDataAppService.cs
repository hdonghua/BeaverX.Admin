using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Oa;

public interface IOaWorkflowDataAppService
{
    Task<PagedResultDto<OaFlowInstanceListDto>> QueryInstancesAsync(OaFlowInstanceQuery input, CancellationToken cancellationToken = default);
    Task TransferAsync(OaTransferRequest input, CancellationToken cancellationToken = default);
}
