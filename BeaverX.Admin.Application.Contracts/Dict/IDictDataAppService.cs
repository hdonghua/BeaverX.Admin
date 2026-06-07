using BeaverX.Admin.Application.Contracts.Dict.Dtos;

namespace BeaverX.Admin.Application.Contracts.Dict;

public interface IDictDataAppService
{
    Task<List<DictDataDto>> GetListAsync(
        DictDataQueryDto input,
        CancellationToken cancellationToken = default);

    Task<List<DictOptionDto>> GetOptionsByTypeCodeAsync(
        string typeCode,
        CancellationToken cancellationToken = default);

    Task<DictDataDto> GetAsync(long id, CancellationToken cancellationToken = default);

    Task<DictDataDto> CreateAsync(CreateDictDataDto input, CancellationToken cancellationToken = default);

    Task<DictDataDto> UpdateAsync(long id, UpdateDictDataDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
