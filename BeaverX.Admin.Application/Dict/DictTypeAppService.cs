using BeaverX.Admin.Application.Contracts.Dict;
using BeaverX.Admin.Application.Contracts.Dict.Dtos;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Rbac;
using BeaverX.Admin.Domain.Dict;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Dict;

public class DictTypeAppService : IDictTypeAppService, IScopedDependency
{
    private readonly IRepository<DictType> _dictTypeRepository;
    private readonly IRepository<DictData> _dictDataRepository;

    public DictTypeAppService(
        IRepository<DictType> dictTypeRepository,
        IRepository<DictData> dictDataRepository)
    {
        _dictTypeRepository = dictTypeRepository;
        _dictDataRepository = dictDataRepository;
    }

    public async Task<PagedResultDto<DictTypeDto>> GetListAsync(
        DictTypeQueryDto input,
        CancellationToken cancellationToken = default)
    {
        var query = _dictTypeRepository.GetQueryable().AsQueryable();

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

    public async Task<DictTypeDto> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _dictTypeRepository.GetAsync(id, cancellationToken);
        return DictMapper.ToDictTypeDto(entity);
    }

    public async Task<DictTypeDto> CreateAsync(
        CreateDictTypeDto input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.Name))
        {
            throw new RbacException("字典类型编码和名称不能为空");
        }

        var code = input.Code.Trim();
        if (await _dictTypeRepository.AnyAsync(x => x.Code == code, cancellationToken))
        {
            throw new RbacException("字典类型编码已存在");
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
        long id,
        UpdateDictTypeDto input,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dictTypeRepository.GetAsync(id, cancellationToken);

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
        return DictMapper.ToDictTypeDto(entity);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        if (await _dictDataRepository.AnyAsync(x => x.DictTypeId == id, cancellationToken))
        {
            throw new RbacException("请先删除该字典类型下的字典数据");
        }

        await _dictTypeRepository.DeleteAsync(id, cancellationToken: cancellationToken);
    }
}
