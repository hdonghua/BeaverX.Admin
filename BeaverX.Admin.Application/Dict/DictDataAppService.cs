using BeaverX.Admin.Application.Caching;
using BeaverX.Admin.Application.Contracts.Caching;
using BeaverX.Admin.Application.Contracts.Dict;
using BeaverX.Admin.Application.Contracts.Dict.Dtos;
using BeaverX.Admin.Domain.Dict;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;

namespace BeaverX.Admin.Application.Dict;

public class DictDataAppService : IDictDataAppService, IScopedDependency
{
    private readonly ISugarRepository<DictType> _dictTypeRepository;
    private readonly ISugarRepository<DictData> _dictDataRepository;
    private readonly ICacheService _cache;
    private readonly AppCacheInvalidator _cacheInvalidator;

    public DictDataAppService(
        ISugarRepository<DictType> dictTypeRepository,
        ISugarRepository<DictData> dictDataRepository,
        ICacheService cache,
        AppCacheInvalidator cacheInvalidator)
    {
        _dictTypeRepository = dictTypeRepository;
        _dictDataRepository = dictDataRepository;
        _cache = cache;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<List<DictDataDto>> GetListAsync(
        DictDataQueryDto input,
        CancellationToken cancellationToken = default)
    {
        var query = _dictDataRepository.GetSugarQueryable();

        if (input.DictTypeId.HasValue)
        {
            query = query.Where(x => x.DictTypeId == input.DictTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.TypeCode))
        {
            var typeCode = input.TypeCode.Trim();
            var dictTypeId = await _dictTypeRepository.GetSugarQueryable()
                .Where(x => x.Code == typeCode)
                .Select(x => x.Id)
                .FirstAsync(cancellationToken);
            query = query.Where(x => x.DictTypeId == dictTypeId);
        }

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            query = query.Where(x =>
                x.Label.Contains(keyword) ||
                x.Value.Contains(keyword));
        }

        if (input.IsEnabled.HasValue)
        {
            query = query.Where(x => x.IsEnabled == input.IsEnabled.Value);
        }

        var items = await query
            .OrderBy(x => x.Sort)
            .OrderByDescending(x => x.CreationTime)
            .ToListAsync(cancellationToken);

        await PopulateDictTypesAsync(items, cancellationToken);
        return items.Select(DictMapper.ToDictDataDto).ToList();
    }

    public Task<List<DictOptionDto>> GetOptionsByTypeCodeAsync(
        string typeCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(typeCode))
        {
            throw new BusinessException("字典类型编码不能为空");
        }

        var code = typeCode.Trim();
        return _cache.GetOrSetAsync(
            CacheKeys.DictOptions(code),
            async ct =>
            {
                var dictType = await _dictTypeRepository.FindAsync(x => x.Code == code && x.IsEnabled, ct);
                if (dictType == null)
                {
                    return [];
                }

                return await _dictDataRepository.GetSugarQueryable()
                .Where(x => x.DictTypeId == dictType.Id && x.IsEnabled)
                .OrderBy(x => x.Sort)
                .OrderBy(x => x.Id)
                .Select(x => new DictOptionDto
                {
                    Label = x.Label,
                    Value = x.Value,
                    ListClass = x.ListClass
                })
                .ToListAsync(ct);
            },
            CacheDurations.Dict,
            cancellationToken);
    }

    public async Task<DictDataDto> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await FindWithTypeAsync(id, cancellationToken);
        return DictMapper.ToDictDataDto(entity);
    }

    public async Task<DictDataDto> CreateAsync(
        CreateDictDataDto input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Label) || string.IsNullOrWhiteSpace(input.Value))
        {
            throw new BusinessException("字典标签和值不能为空");
        }

        await EnsureDictTypeExistsAsync(input.DictTypeId, cancellationToken);

        var value = input.Value.Trim();
        if (await _dictDataRepository.AnyAsync(
                x => x.DictTypeId == input.DictTypeId && x.Value == value,
                cancellationToken))
        {
            throw new BusinessException("该字典类型下已存在相同的字典值");
        }

        var entity = new DictData
        {
            DictTypeId = input.DictTypeId,
            Label = input.Label.Trim(),
            Value = value,
            Sort = input.Sort,
            IsEnabled = input.IsEnabled,
            CssClass = input.CssClass?.Trim(),
            ListClass = input.ListClass?.Trim(),
            Remark = input.Remark?.Trim()
        };

        await _dictDataRepository.InsertAsync(entity, cancellationToken: cancellationToken);
        var result = DictMapper.ToDictDataDto(await FindWithTypeAsync(entity.Id, cancellationToken));
        await _cacheInvalidator.InvalidateDictOptionsAsync(result.DictTypeCode, cancellationToken);
        return result;
    }

    public async Task<DictDataDto> UpdateAsync(
        long id,
        UpdateDictDataDto input,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindWithTypeAsync(id, cancellationToken);
        var typeCode = entity.DictType.Code;

        if (input.Label != null)
        {
            entity.Label = input.Label.Trim();
        }

        if (input.Value != null)
        {
            var value = input.Value.Trim();
            if (await _dictDataRepository.AnyAsync(
                    x => x.DictTypeId == entity.DictTypeId && x.Value == value && x.Id != id,
                    cancellationToken))
            {
                throw new BusinessException("该字典类型下已存在相同的字典值");
            }

            entity.Value = value;
        }

        if (input.Sort.HasValue)
        {
            entity.Sort = input.Sort.Value;
        }

        if (input.IsEnabled.HasValue)
        {
            entity.IsEnabled = input.IsEnabled.Value;
        }

        if (input.CssClass != null)
        {
            entity.CssClass = string.IsNullOrWhiteSpace(input.CssClass) ? null : input.CssClass.Trim();
        }

        if (input.ListClass != null)
        {
            entity.ListClass = string.IsNullOrWhiteSpace(input.ListClass) ? null : input.ListClass.Trim();
        }

        if (input.Remark != null)
        {
            entity.Remark = string.IsNullOrWhiteSpace(input.Remark) ? null : input.Remark.Trim();
        }

        await _dictDataRepository.UpdateAsync(entity, cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateDictOptionsAsync(typeCode, cancellationToken);
        return DictMapper.ToDictDataDto(entity);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await FindWithTypeAsync(id, cancellationToken);
        var typeCode = entity.DictType.Code;
        await _dictDataRepository.DeleteAsync(id, cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateDictOptionsAsync(typeCode, cancellationToken);
    }

    private async Task EnsureDictTypeExistsAsync(long dictTypeId, CancellationToken cancellationToken)
    {
        if (!await _dictTypeRepository.AnyAsync(x => x.Id == dictTypeId, cancellationToken))
        {
            throw new BusinessException("字典类型不存在");
        }
    }

    private async Task<DictData> FindWithTypeAsync(long id, CancellationToken cancellationToken)
    {
        var entity = await _dictDataRepository.FindAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
        {
            throw new BusinessException($"字典数据不存在: {id}");
        }

        entity.DictType = await _dictTypeRepository.GetAsync(entity.DictTypeId, cancellationToken);
        return entity;
    }

    private async Task PopulateDictTypesAsync(List<DictData> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        var dictTypeIds = items.Select(x => x.DictTypeId).Distinct().ToList();
        var dictTypes = await _dictTypeRepository.GetSugarQueryable()
            .Where(x => dictTypeIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        var dictTypeMap = dictTypes.ToDictionary(x => x.Id);

        foreach (var item in items)
        {
            if (dictTypeMap.TryGetValue(item.DictTypeId, out var dictType))
            {
                item.DictType = dictType;
            }
        }
    }
}
