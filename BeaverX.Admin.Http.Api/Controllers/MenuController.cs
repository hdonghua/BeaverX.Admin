using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Admin.Http.Api.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

public class MenuController : AdminControllerBase
{
    private readonly IMenuAppService _menuAppService;

    public MenuController(IMenuAppService menuAppService)
    {
        _menuAppService = menuAppService;
    }

    [RequirePermission(
        RbacPermissionCodes.System.Menu.List,
        RbacPermissionCodes.System.Role.AssignMenus)]
    [HttpGet("tree")]
    public Task<List<MenuDto>> GetTreeAsync(CancellationToken cancellationToken)
        => _menuAppService.GetTreeAsync(cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Menu.List)]
    [HttpGet("{id:guid}")]
    public Task<MenuDto> GetAsync(Guid id, CancellationToken cancellationToken)
        => _menuAppService.GetAsync(id, cancellationToken: cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Menu.Create)]
    [HttpPost]
    public Task<MenuDto> CreateAsync([FromBody] CreateMenuDto input, CancellationToken cancellationToken)
        => _menuAppService.CreateAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Menu.Update)]
    [HttpPut("{id:guid}")]
    public Task<MenuDto> UpdateAsync(Guid id, [FromBody] UpdateMenuDto input, CancellationToken cancellationToken)
        => _menuAppService.UpdateAsync(id, input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Menu.Update)]
    [HttpPut("reorder")]
    public Task ReorderAsync([FromBody] ReorderMenusDto input, CancellationToken cancellationToken)
        => _menuAppService.ReorderAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Menu.Delete)]
    [HttpDelete("{id:guid}")]
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        => _menuAppService.DeleteAsync(id, cancellationToken: cancellationToken);
}
