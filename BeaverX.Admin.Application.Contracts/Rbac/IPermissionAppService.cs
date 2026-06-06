using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Rbac;

public interface IPermissionAppService
{
    Task<List<PermissionDto>> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<PermissionDto> GetAsync(long id, CancellationToken cancellationToken = default);
    Task<PermissionDto> CreateAsync(CreatePermissionDto input, CancellationToken cancellationToken = default);
    Task<PermissionDto> UpdateAsync(long id, UpdatePermissionDto input, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
