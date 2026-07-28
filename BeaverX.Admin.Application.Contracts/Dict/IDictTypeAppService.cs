using BeaverX.Admin.Application.Contracts.Dict.Dtos;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Dict;

public interface IDictTypeAppService
{
    Task<PagedResultDto<DictTypeDto>> GetListAsync(
        DictTypeQueryDto input,
        CancellationToken cancellationToken = default);

    Task<DictTypeDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DictTypeDto> CreateAsync(CreateDictTypeDto input, CancellationToken cancellationToken = default);

    Task<DictTypeDto> UpdateAsync(Guid id, UpdateDictTypeDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
