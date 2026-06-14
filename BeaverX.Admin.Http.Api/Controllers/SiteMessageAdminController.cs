using BeaverX.Admin.Application.Contracts.Messages;
using BeaverX.Admin.Application.Contracts.Messages.Dtos;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Admin.Http.Api.Authorization;
using BeaverX.WebMvc.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

public class SiteMessageAdminController : BeaverXControllerBase
{
    private readonly ISiteMessageAdminAppService _siteMessageAdminAppService;

    public SiteMessageAdminController(ISiteMessageAdminAppService siteMessageAdminAppService)
    {
        _siteMessageAdminAppService = siteMessageAdminAppService;
    }

    [RequirePermission(RbacPermissionCodes.System.Message.Send)]
    [HttpPost("send")]
    public Task<SendSiteMessageResultDto> SendAsync(
        [FromBody] SendSiteMessageDto input,
        CancellationToken cancellationToken)
        => _siteMessageAdminAppService.SendAsync(input, cancellationToken);
}
