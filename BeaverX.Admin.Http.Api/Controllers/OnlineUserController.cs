using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Admin.Http.Api.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

public class OnlineUserController : AdminControllerBase
{
    private readonly IOnlineUserAppService _onlineUserAppService;

    public OnlineUserController(IOnlineUserAppService onlineUserAppService)
    {
        _onlineUserAppService = onlineUserAppService;
    }

    [RequirePermission(RbacPermissionCodes.System.OnlineUser.List)]
    [HttpGet("list")]
    public Task<List<OnlineUserDto>> GetListAsync(CancellationToken cancellationToken)
        => _onlineUserAppService.GetListAsync(cancellationToken: cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.OnlineUser.Kick)]
    [HttpPost("{userId:guid}/kick")]
    public Task KickAsync(Guid userId, CancellationToken cancellationToken)
        => _onlineUserAppService.KickAsync(userId, cancellationToken);
}
