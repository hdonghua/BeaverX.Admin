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

    Task<DictDataDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DictDataDto> CreateAsync(CreateDictDataDto input, CancellationToken cancellationToken = default);

    Task<DictDataDto> UpdateAsync(Guid id, UpdateDictDataDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
