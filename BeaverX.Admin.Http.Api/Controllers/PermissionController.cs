using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Admin.Http.Api.Authorization;
using BeaverX.WebMvc.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

public class PermissionController : BeaverXController
{
    private readonly IPermissionAppService _permissionAppService;

    public PermissionController(IPermissionAppService permissionAppService)
    {
        _permissionAppService = permissionAppService;
    }

    [RequirePermission(RbacPermissionCodes.System.Permission.List)]
    [HttpGet("tree")]
    public Task<List<PermissionDto>> GetTreeAsync(CancellationToken cancellationToken)
        => _permissionAppService.GetTreeAsync(cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Permission.List)]
    [HttpGet("{id:long}")]
    public Task<PermissionDto> GetAsync(long id, CancellationToken cancellationToken)
        => _permissionAppService.GetAsync(id, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Permission.Create)]
    [HttpPost]
    public Task<PermissionDto> CreateAsync([FromBody] CreatePermissionDto input, CancellationToken cancellationToken)
        => _permissionAppService.CreateAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Permission.Update)]
    [HttpPut("{id:long}")]
    public Task<PermissionDto> UpdateAsync(long id, [FromBody] UpdatePermissionDto input, CancellationToken cancellationToken)
        => _permissionAppService.UpdateAsync(id, input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Permission.Delete)]
    [HttpDelete("{id:long}")]
    public Task DeleteAsync(long id, CancellationToken cancellationToken)
        => _permissionAppService.DeleteAsync(id, cancellationToken);
}
