using BeaverX.Admin.Application.Contracts.Config.Dtos;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Config;

public interface IConfigAppService
{
    Task<PagedResultDto<ConfigDto>> GetListAsync(
        ConfigQueryDto input,
        CancellationToken cancellationToken = default);

    Task<List<string>> GetGroupsAsync(CancellationToken cancellationToken = default);

    Task<ConfigDto> GetAsync(long id, CancellationToken cancellationToken = default);

    Task<ConfigDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task<ConfigDto> CreateAsync(CreateConfigDto input, CancellationToken cancellationToken = default);

    Task<ConfigDto> UpdateAsync(long id, UpdateConfigDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
