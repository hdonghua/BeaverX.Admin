using BeaverX.Admin.Application.Contracts.Oa;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

[Authorize]
public class OaOrganizationController : AdminControllerBase
{
    private readonly IOaOrganizationAppService _service;

    public OaOrganizationController(IOaOrganizationAppService service) => _service = service;

    [HttpGet("options")]
    public Task<OaOrganizationOptionsDto> GetOptionsAsync(CancellationToken cancellationToken) =>
        _service.GetOptionsAsync(cancellationToken);
}
