using BeaverX.Admin.Application.Caching;
using BeaverX.Admin.Application.Contracts.Caching;
using BeaverX.Admin.Application.Contracts.Dict;
using BeaverX.Admin.Application.Contracts.Dict.Dtos;
using BeaverX.Admin.Domain.Dict;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Dict;

public class DictDataAppService : IDictDataAppService, IScopedDependency
{
    private readonly IRepository<DictType, Guid> _dictTypeRepository;
    private readonly IRepository<DictData, Guid> _dictDataRepository;
    private readonly ICacheService _cache;
    private readonly AppCacheInvalidator _cacheInvalidator;

    public DictDataAppService(
        IRepository<DictType, Guid> dictTypeRepository,
        IRepository<DictData, Guid> dictDataRepository,
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
        var query = (await _dictDataRepository.GetQueryableAsync())
            .Include(x => x.DictType)
            .AsQueryable();

        if (input.DictTypeId.HasValue)
        {
            query = query.Where(x => x.DictTypeId == input.DictTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.TypeCode))
        {
            var typeCode = input.TypeCode.Trim();
            query = query.Where(x => x.DictType.Code == typeCode);
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
            .ThenByDescending(x => x.CreationTime)
            .ToListAsync(cancellationToken);

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
            async ct => await (await _dictDataRepository.GetQueryableAsync())
                .Include(x => x.DictType)
                .Where(x => x.DictType.Code == code && x.IsEnabled && x.DictType.IsEnabled)
                .OrderBy(x => x.Sort)
                .ThenBy(x => x.Id)
                .Select(x => new DictOptionDto
                {
                    Label = x.Label,
                    Value = x.Value,
                    ListClass = x.ListClass
                })
                .ToListAsync(ct),
            CacheDurations.Dict,
            cancellationToken);
    }

    public async Task<DictDataDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
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

        var dictType = await _dictTypeRepository.GetAsync(input.DictTypeId, cancellationToken: cancellationToken);

        var value = input.Value.Trim();
        if (await _dictDataRepository.AnyAsync(
                x => x.DictTypeId == input.DictTypeId && x.Value == value, cancellationToken))
        {
            throw new BusinessException("该字典类型下已存在相同的字典值");
        }

        var entity = new DictData
        {
            DictTypeId = input.DictTypeId,
            DictType = dictType,
            Label = input.Label.Trim(),
            Value = value,
            Sort = input.Sort,
            IsEnabled = input.IsEnabled,
            CssClass = input.CssClass?.Trim(),
            ListClass = input.ListClass?.Trim(),
            Remark = input.Remark?.Trim()
        };

        await _dictDataRepository.InsertAsync(entity, cancellationToken: cancellationToken);
        var result = DictMapper.ToDictDataDto(entity);
        await _cacheInvalidator.InvalidateDictOptionsAsync(result.DictTypeCode, cancellationToken);
        return result;
    }

    public async Task<DictDataDto> UpdateAsync(
        Guid id,
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
                    x => x.DictTypeId == entity.DictTypeId && x.Value == value && x.Id != id, cancellationToken))
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

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await FindWithTypeAsync(id, cancellationToken);
        var typeCode = entity.DictType.Code;
        await _dictDataRepository.DeleteAsync(id, cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateDictOptionsAsync(typeCode, cancellationToken);
    }

    private async Task<DictData> FindWithTypeAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await (await _dictDataRepository.GetQueryableAsync())
            .Include(x => x.DictType)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
        {
            throw new BusinessException($"字典数据不存在: {id}");
        }

        return entity;
    }
}
