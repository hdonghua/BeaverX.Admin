using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Admin.Http.Api.Authorization;
using BeaverX.WebMvc.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

public class UserController : BeaverXController
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
    [HttpGet("{id:long}")]
    public Task<UserDto> GetAsync(long id, CancellationToken cancellationToken)
        => _userAppService.GetAsync(id, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.User.Create)]
    [HttpPost]
    public Task<UserDto> CreateAsync([FromBody] CreateUserDto input, CancellationToken cancellationToken)
        => _userAppService.CreateAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.User.Update)]
    [HttpPut("{id:long}")]
    public Task<UserDto> UpdateAsync(long id, [FromBody] UpdateUserDto input, CancellationToken cancellationToken)
        => _userAppService.UpdateAsync(id, input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.User.Delete)]
    [HttpDelete("{id:long}")]
    public Task DeleteAsync(long id, CancellationToken cancellationToken)
        => _userAppService.DeleteAsync(id, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.User.AssignRoles)]
    [HttpPut("{id:long}/roles")]
    public Task AssignRolesAsync(long id, [FromBody] AssignUserRolesDto input, CancellationToken cancellationToken)
        => _userAppService.AssignRolesAsync(id, input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.User.ResetPassword)]
    [HttpPut("{id:long}/password")]
    public Task ResetPasswordAsync(long id, [FromBody] ResetPasswordDto input, CancellationToken cancellationToken)
        => _userAppService.ResetPasswordAsync(id, input, cancellationToken);
}
