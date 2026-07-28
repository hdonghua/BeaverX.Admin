using BeaverX.Admin.Application.Contracts.Config.Dtos;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Config;

public interface IConfigAppService
{
    Task<PagedResultDto<ConfigDto>> GetListAsync(
        ConfigQueryDto input,
        CancellationToken cancellationToken = default);

    Task<List<string>> GetGroupsAsync(CancellationToken cancellationToken = default);

    Task<ConfigDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ConfigDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task<ConfigDto> CreateAsync(CreateConfigDto input, CancellationToken cancellationToken = default);

    Task<ConfigDto> UpdateAsync(Guid id, UpdateConfigDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
