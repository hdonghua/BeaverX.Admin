using BeaverX.Admin.Application.Caching;
using BeaverX.Admin.Application.Contracts.Dict;
using BeaverX.Admin.Application.Contracts.Dict.Dtos;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Rbac;
using BeaverX.Admin.Domain.Dict;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Dict;

public class DictTypeAppService : IDictTypeAppService, IScopedDependency
{
    private readonly IRepository<DictType, Guid> _dictTypeRepository;
    private readonly IRepository<DictData, Guid> _dictDataRepository;
    private readonly AppCacheInvalidator _cacheInvalidator;

    public DictTypeAppService(
        IRepository<DictType, Guid> dictTypeRepository,
        IRepository<DictData, Guid> dictDataRepository,
        AppCacheInvalidator cacheInvalidator)
    {
        _dictTypeRepository = dictTypeRepository;
        _dictDataRepository = dictDataRepository;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<PagedResultDto<DictTypeDto>> GetListAsync(
        DictTypeQueryDto input,
        CancellationToken cancellationToken = default)
    {
        var query = (await _dictTypeRepository.GetQueryableAsync()).AsQueryable();

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            query = query.Where(x =>
                x.Code.Contains(keyword) ||
                x.Name.Contains(keyword));
        }

        if (input.IsEnabled.HasValue)
        {
            query = query.Where(x => x.IsEnabled == input.IsEnabled.Value);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var (skip, take) = RbacQueryHelper.GetPaging(input.Page, input.PageSize);
        var items = await query
            .OrderByDescending(x => x.CreationTime)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<DictTypeDto>
        {
            Total = total,
            Items = items.Select(DictMapper.ToDictTypeDto).ToList()
        };
    }

    public async Task<DictTypeDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dictTypeRepository.GetAsync(id, cancellationToken: cancellationToken);
        return DictMapper.ToDictTypeDto(entity);
    }

    public async Task<DictTypeDto> CreateAsync(
        CreateDictTypeDto input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.Name))
        {
            throw new BusinessException("字典类型编码和名称不能为空");
        }

        var code = input.Code.Trim();
        if (await _dictTypeRepository.AnyAsync(x => x.Code == code, cancellationToken))
        {
            throw new BusinessException("字典类型编码已存在");
        }

        var entity = new DictType
        {
            Code = code,
            Name = input.Name.Trim(),
            Remark = input.Remark?.Trim(),
            IsEnabled = input.IsEnabled
        };

        await _dictTypeRepository.InsertAsync(entity, cancellationToken: cancellationToken);
        return DictMapper.ToDictTypeDto(entity);
    }

    public async Task<DictTypeDto> UpdateAsync(
        Guid id,
        UpdateDictTypeDto input,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dictTypeRepository.GetAsync(id, cancellationToken: cancellationToken);

        if (input.Name != null)
        {
            entity.Name = input.Name.Trim();
        }

        if (input.Remark != null)
        {
            entity.Remark = string.IsNullOrWhiteSpace(input.Remark) ? null : input.Remark.Trim();
        }

        if (input.IsEnabled.HasValue)
        {
            entity.IsEnabled = input.IsEnabled.Value;
        }

        await _dictTypeRepository.UpdateAsync(entity, cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateDictOptionsAsync(entity.Code, cancellationToken);
        return DictMapper.ToDictTypeDto(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dictTypeRepository.GetAsync(id, cancellationToken: cancellationToken);

        if (await _dictDataRepository.AnyAsync(x => x.DictTypeId == id, cancellationToken))
        {
            throw new BusinessException("请先删除该字典类型下的字典数据");
        }

        await _dictTypeRepository.DeleteAsync(id, cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateDictOptionsAsync(entity.Code, cancellationToken);
    }
}
