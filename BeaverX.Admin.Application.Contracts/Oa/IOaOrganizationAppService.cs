namespace BeaverX.Admin.Application.Contracts.Oa;

public interface IOaOrganizationAppService
{
    Task<OaOrganizationOptionsDto> GetOptionsAsync(CancellationToken cancellationToken = default);
}
