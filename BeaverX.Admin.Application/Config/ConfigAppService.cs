using BeaverX.Admin.Application.Caching;
using BeaverX.Admin.Application.Contracts.Caching;
using BeaverX.Admin.Application.Contracts.Config;
using BeaverX.Admin.Application.Contracts.Config.Dtos;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Rbac;
using BeaverX.Admin.Domain.Config;
using BeaverX.Core.Dependency;
using BeaverX.Data.SqlSugar.Repositories;
using SqlSugar;
using ICacheService = BeaverX.Admin.Application.Contracts.Caching.ICacheService;

namespace BeaverX.Admin.Application.Config;

public class ConfigAppService : IConfigAppService, IScopedDependency
{
    private readonly ISugarRepository<SysConfig> _configRepository;
    private readonly ICacheService _cache;
    private readonly AppCacheInvalidator _cacheInvalidator;

    public ConfigAppService(
        ISugarRepository<SysConfig> configRepository,
        ICacheService cache,
        AppCacheInvalidator cacheInvalidator)
    {
        _configRepository = configRepository;
        _cache = cache;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<PagedResultDto<ConfigDto>> GetListAsync(
        ConfigQueryDto input,
        CancellationToken cancellationToken = default)
    {
        var query = _configRepository.GetSugarQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.Keyword), x =>
                x.Key.Contains(input.Keyword!) ||
                x.Label.Contains(input.Keyword!) ||
                x.Value.Contains(input.Keyword!))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Group), x => x.Group == input.Group!.Trim())
            .WhereIF(input.IsEnabled.HasValue, x => x.IsEnabled == input.IsEnabled!.Value);

        RefAsync<int> total = 0;
        var (skip, take) = RbacQueryHelper.GetPaging(input.Page, input.PageSize);
        var items = await query
            .OrderBy(x => x.Group)
            .OrderBy(x => x.Sort)
            .OrderByDescending(x => x.CreationTime)
            .ToOffsetPageAsync(input.Page, input.PageSize, total, cancellationToken);

        return new PagedResultDto<ConfigDto>
        {
            Total = total,
            Items = items.Select(ToDto).ToList()
        };
    }

    public Task<List<string>> GetGroupsAsync(CancellationToken cancellationToken = default) =>
        _cache.GetOrSetAsync(
            CacheKeys.ConfigGroups,
            async ct => await _configRepository.GetSugarQueryable()
                .Where(x => !string.IsNullOrEmpty(x.Group))
                .OrderBy(x => x.Group)
                .Select(x => x.Group!)
                .Distinct()
                .ToListAsync(ct),
            CacheDurations.Config,
            cancellationToken);

    public async Task<ConfigDto> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        return ToDto(entity);
    }

    public async Task<ConfigDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new BusinessException("配置键不能为空");
        }

        var normalizedKey = key.Trim();
        return await _cache.GetOrSetAsync(
            CacheKeys.ConfigByKey(normalizedKey),
            async ct =>
            {
                var entity = await _configRepository.GetSugarQueryable()
                    .FirstAsync(x => x.Key == normalizedKey, ct);

                return entity == null ? null : ToDto(entity);
            },
            CacheDurations.Config,
            cancellationToken);
    }

    public async Task<ConfigDto> CreateAsync(
        CreateConfigDto input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Key) ||
            string.IsNullOrWhiteSpace(input.Value) ||
            string.IsNullOrWhiteSpace(input.Label))
        {
            throw new BusinessException("配置键、标签和值不能为空");
        }

        var key = input.Key.Trim();
        if (await _configRepository.AnyAsync(x => x.Key == key, cancellationToken))
        {
            throw new BusinessException("配置键已存在");
        }

        var entity = new SysConfig
        {
            Key = key,
            Value = input.Value.Trim(),
            Label = input.Label.Trim(),
            Group = NormalizeGroup(input.Group),
            Remark = input.Remark?.Trim(),
            Sort = input.Sort,
            IsEnabled = input.IsEnabled
        };

        await _configRepository.InsertAsync(entity, cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateConfigAsync(key, cancellationToken);
        return ToDto(entity);
    }

    public async Task<ConfigDto> UpdateAsync(
        long id,
        UpdateConfigDto input,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);

        if (input.Value != null)
        {
            entity.Value = input.Value.Trim();
        }

        if (input.Label != null)
        {
            entity.Label = input.Label.Trim();
        }

        if (input.Group != null)
        {
            entity.Group = NormalizeGroup(input.Group);
        }

        if (input.Remark != null)
        {
            entity.Remark = string.IsNullOrWhiteSpace(input.Remark) ? null : input.Remark.Trim();
        }

        if (input.Sort.HasValue)
        {
            entity.Sort = input.Sort.Value;
        }

        if (input.IsEnabled.HasValue)
        {
            entity.IsEnabled = input.IsEnabled.Value;
        }

        await _configRepository.UpdateAsync(entity, cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateConfigAsync(entity.Key, cancellationToken);
        return ToDto(entity);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        await _configRepository.DeleteAsync(id, cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateConfigAsync(entity.Key, cancellationToken);
    }

    private async Task<SysConfig> FindAsync(long id, CancellationToken cancellationToken)
    {
        var entity = await _configRepository.FindAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            throw new BusinessException($"配置不存在: {id}");
        }

        return entity;
    }

    private static string? NormalizeGroup(string? group)
    {
        return string.IsNullOrWhiteSpace(group) ? null : group.Trim();
    }

    private static ConfigDto ToDto(SysConfig entity) => new()
    {
        Id = entity.Id,
        Key = entity.Key,
        Value = entity.Value,
        Label = entity.Label,
        Group = entity.Group,
        Remark = entity.Remark,
        Sort = entity.Sort,
        IsEnabled = entity.IsEnabled,
        CreationTime = entity.CreationTime
    };
}
