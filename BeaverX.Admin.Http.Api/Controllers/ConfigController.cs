using BeaverX.Admin.Application.Contracts.Config;
using BeaverX.Admin.Application.Contracts.Config.Dtos;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Admin.Http.Api.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

public class ConfigController : AdminControllerBase
{
    private readonly IConfigAppService _configAppService;

    public ConfigController(IConfigAppService configAppService)
    {
        _configAppService = configAppService;
    }

    [RequirePermission(RbacPermissionCodes.System.Config.List)]
    [HttpGet("list")]
    public Task<PagedResultDto<ConfigDto>> GetListAsync(
        [FromQuery] ConfigQueryDto input,
        CancellationToken cancellationToken)
        => _configAppService.GetListAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Config.List)]
    [HttpGet("groups")]
    public Task<List<string>> GetGroupsAsync(CancellationToken cancellationToken)
        => _configAppService.GetGroupsAsync(cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Config.List)]
    [HttpGet("{id:guid}")]
    public Task<ConfigDto> GetAsync(Guid id, CancellationToken cancellationToken)
        => _configAppService.GetAsync(id, cancellationToken: cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Config.List)]
    [HttpGet("key/{key}")]
    public async Task<ConfigDto> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var config = await _configAppService.GetByKeyAsync(key, cancellationToken);
        if (config == null)
        {
            throw new BusinessException($"配置不存在: {key}");
        }

        return config;
    }

    [RequirePermission(RbacPermissionCodes.System.Config.Create)]
    [HttpPost]
    public Task<ConfigDto> CreateAsync(
        [FromBody] CreateConfigDto input,
        CancellationToken cancellationToken)
        => _configAppService.CreateAsync(input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Config.Update)]
    [HttpPut("{id:guid}")]
    public Task<ConfigDto> UpdateAsync(
        Guid id,
        [FromBody] UpdateConfigDto input,
        CancellationToken cancellationToken)
        => _configAppService.UpdateAsync(id, input, cancellationToken);

    [RequirePermission(RbacPermissionCodes.System.Config.Delete)]
    [HttpDelete("{id:guid}")]
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        => _configAppService.DeleteAsync(id, cancellationToken: cancellationToken);
}
