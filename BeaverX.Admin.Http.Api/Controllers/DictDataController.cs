using BeaverX.Admin.Application.Contracts.Dict;
using BeaverX.Admin.Application.Contracts.Dict.Dtos;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Admin.Http.Api.Authorization;
using BeaverX.WebMvc.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

public class DictDataController : BeaverXControllerBase
{
    private readonly IDictDataAppService _dictDataAppService;

    public DictDataController(IDictDataAppService dictDataAppService)
    {
        _dictDataAppService = dictDataAppService;
    }

    [RequirePermission(RbacPermissionCodes.System.Dict.List)]
    [HttpGet("list")]
    public Task<List<DictDataDto>> GetListAsync(
        [FromQuery] DictDataQueryDto input,
        CancellationToken cancellationToken)
        => _dictDataAppService.GetListAsync(input, cancellationToken);

    [AllowAnonymous]
    [HttpGet("options/{typeCode}")]
    public Task<List<DictOptionDto>> GetOptionsByTypeCodeAsync(
        string typeCode,
        CancellationToken cancellationToken)
        => _dictDataAppService.GetOptionsByTypeCodeAsync(typeCode, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Dict.List)]
    [HttpGet("{id:long}")]
    public Task<DictDataDto> GetAsync(long id, CancellationToken cancellationToken)
        => _dictDataAppService.GetAsync(id, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Dict.Data.Create)]
    [HttpPost]
    public Task<DictDataDto> CreateAsync(
        [FromBody] CreateDictDataDto input,
        CancellationToken cancellationToken)
        => _dictDataAppService.CreateAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Dict.Data.Update)]
    [HttpPut("{id:long}")]
    public Task<DictDataDto> UpdateAsync(
        long id,
        [FromBody] UpdateDictDataDto input,
        CancellationToken cancellationToken)
        => _dictDataAppService.UpdateAsync(id, input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Dict.Data.Delete)]
    [HttpDelete("{id:long}")]
    public Task DeleteAsync(long id, CancellationToken cancellationToken)
        => _dictDataAppService.DeleteAsync(id, cancellationToken);
}
