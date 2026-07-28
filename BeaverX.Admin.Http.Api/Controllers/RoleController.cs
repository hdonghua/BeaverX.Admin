using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Admin.Http.Api.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

public class RoleController : AdminControllerBase
{
    private readonly IRoleAppService _roleAppService;

    public RoleController(IRoleAppService roleAppService)
    {
        _roleAppService = roleAppService;
    }

    [RequirePermission(
        RbacPermissionCodes.System.Role.List,
        RbacPermissionCodes.System.User.AssignRoles)]
    [HttpGet("list")]
    public Task<PagedResultDto<RoleDto>> GetListAsync([FromQuery] RoleQueryDto input, CancellationToken cancellationToken)
        => _roleAppService.GetListAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Role.List)]
    [HttpGet("{id:guid}")]
    public Task<RoleDto> GetAsync(Guid id, CancellationToken cancellationToken)
        => _roleAppService.GetAsync(id, cancellationToken: cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Role.Create)]
    [HttpPost]
    public Task<RoleDto> CreateAsync([FromBody] CreateRoleDto input, CancellationToken cancellationToken)
        => _roleAppService.CreateAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Role.Update)]
    [HttpPut("{id:guid}")]
    public Task<RoleDto> UpdateAsync(Guid id, [FromBody] UpdateRoleDto input, CancellationToken cancellationToken)
        => _roleAppService.UpdateAsync(id, input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Role.Delete)]
    [HttpDelete("{id:guid}")]
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        => _roleAppService.DeleteAsync(id, cancellationToken: cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Role.AssignMenus)]
    [HttpPut("{id:guid}/menus")]
    public Task AssignMenusAsync(Guid id, [FromBody] AssignRoleMenusDto input, CancellationToken cancellationToken)
        => _roleAppService.AssignMenusAsync(id, input, cancellationToken);
}
