using BeaverX.Admin.Application.Contracts.Dict;
using BeaverX.Admin.Application.Contracts.Dict.Dtos;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Admin.Http.Api.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

public class DictTypeController : AdminControllerBase
{
    private readonly IDictTypeAppService _dictTypeAppService;

    public DictTypeController(IDictTypeAppService dictTypeAppService)
    {
        _dictTypeAppService = dictTypeAppService;
    }

    [RequirePermission(RbacPermissionCodes.System.Dict.List)]
    [HttpGet("list")]
    public Task<PagedResultDto<DictTypeDto>> GetListAsync(
        [FromQuery] DictTypeQueryDto input,
        CancellationToken cancellationToken)
        => _dictTypeAppService.GetListAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Dict.List)]
    [HttpGet("{id:guid}")]
    public Task<DictTypeDto> GetAsync(Guid id, CancellationToken cancellationToken)
        => _dictTypeAppService.GetAsync(id, cancellationToken: cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Dict.Type.Create)]
    [HttpPost]
    public Task<DictTypeDto> CreateAsync(
        [FromBody] CreateDictTypeDto input,
        CancellationToken cancellationToken)
        => _dictTypeAppService.CreateAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Dict.Type.Update)]
    [HttpPut("{id:guid}")]
    public Task<DictTypeDto> UpdateAsync(
        Guid id,
        [FromBody] UpdateDictTypeDto input,
        CancellationToken cancellationToken)
        => _dictTypeAppService.UpdateAsync(id, input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Dict.Type.Delete)]
    [HttpDelete("{id:guid}")]
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        => _dictTypeAppService.DeleteAsync(id, cancellationToken: cancellationToken);
}
