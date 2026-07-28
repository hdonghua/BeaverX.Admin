using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Rbac;

public interface IRoleAppService
{
    Task<PagedResultDto<RoleDto>> GetListAsync(RoleQueryDto input, CancellationToken cancellationToken = default);
    Task<RoleDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RoleDto> CreateAsync(CreateRoleDto input, CancellationToken cancellationToken = default);
    Task<RoleDto> UpdateAsync(Guid id, UpdateRoleDto input, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task AssignMenusAsync(Guid id, AssignRoleMenusDto input, CancellationToken cancellationToken = default);
}
