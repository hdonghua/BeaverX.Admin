using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Rbac;

public interface IRoleAppService
{
    Task<PagedResultDto<RoleDto>> GetListAsync(RoleQueryDto input, CancellationToken cancellationToken = default);
    Task<RoleDto> GetAsync(long id, CancellationToken cancellationToken = default);
    Task<RoleDto> CreateAsync(CreateRoleDto input, CancellationToken cancellationToken = default);
    Task<RoleDto> UpdateAsync(long id, UpdateRoleDto input, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task AssignMenusAsync(long id, AssignRoleMenusDto input, CancellationToken cancellationToken = default);
}
