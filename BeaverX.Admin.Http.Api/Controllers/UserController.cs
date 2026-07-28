using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Admin.Http.Api.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

public class UserController : AdminControllerBase
{
    private readonly IUserAppService _userAppService;

    public UserController(IUserAppService userAppService)
    {
        _userAppService = userAppService;
    }

    [RequirePermission(RbacPermissionCodes.System.User.List)]
    [HttpGet("list")]
    public Task<PagedResultDto<UserDto>> GetListAsync([FromQuery] UserQueryDto input, CancellationToken cancellationToken)
        => _userAppService.GetListAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.User.List)]
    [HttpGet("{id:guid}")]
    public Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken)
        => _userAppService.GetAsync(id, cancellationToken: cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.User.Create)]
    [HttpPost]
    public Task<UserDto> CreateAsync([FromBody] CreateUserDto input, CancellationToken cancellationToken)
        => _userAppService.CreateAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.User.Update)]
    [HttpPut("{id:guid}")]
    public Task<UserDto> UpdateAsync(Guid id, [FromBody] UpdateUserDto input, CancellationToken cancellationToken)
        => _userAppService.UpdateAsync(id, input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.User.Delete)]
    [HttpDelete("{id:guid}")]
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        => _userAppService.DeleteAsync(id, cancellationToken: cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.User.AssignRoles)]
    [HttpPut("{id:guid}/roles")]
    public Task AssignRolesAsync(Guid id, [FromBody] AssignUserRolesDto input, CancellationToken cancellationToken)
        => _userAppService.AssignRolesAsync(id, input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.User.ResetPassword)]
    [HttpPut("{id:guid}/password")]
    public Task ResetPasswordAsync(Guid id, [FromBody] ResetPasswordDto input, CancellationToken cancellationToken)
        => _userAppService.ResetPasswordAsync(id, input, cancellationToken);
}
