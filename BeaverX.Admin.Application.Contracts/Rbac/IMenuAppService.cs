using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Rbac;

public interface IMenuAppService
{
    Task<List<MenuDto>> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<MenuDto> GetAsync(long id, CancellationToken cancellationToken = default);
    Task<MenuDto> CreateAsync(CreateMenuDto input, CancellationToken cancellationToken = default);
    Task<MenuDto> UpdateAsync(long id, UpdateMenuDto input, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
