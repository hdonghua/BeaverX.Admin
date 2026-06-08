using BeaverX.Admin.Application.Contracts.Config;
using BeaverX.Admin.Application.Contracts.Config.Dtos;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Rbac;
using BeaverX.Admin.Domain.Config;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Config;

public class ConfigAppService : IConfigAppService, IScopedDependency
{
    private readonly IRepository<SysConfig> _configRepository;

    public ConfigAppService(IRepository<SysConfig> configRepository)
    {
        _configRepository = configRepository;
    }

    public async Task<PagedResultDto<ConfigDto>> GetListAsync(
        ConfigQueryDto input,
        CancellationToken cancellationToken = default)
    {
        var query = _configRepository.GetQueryable().AsQueryable();

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            query = query.Where(x =>
                x.Key.Contains(keyword) ||
                x.Label.Contains(keyword) ||
                x.Value.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(input.Group))
        {
            var group = input.Group.Trim();
            query = query.Where(x => x.Group == group);
        }

        if (input.IsEnabled.HasValue)
        {
            query = query.Where(x => x.IsEnabled == input.IsEnabled.Value);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var (skip, take) = RbacQueryHelper.GetPaging(input.Page, input.PageSize);
        var items = await query
            .OrderBy(x => x.Group)
            .ThenBy(x => x.Sort)
            .ThenByDescending(x => x.CreationTime)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<ConfigDto>
        {
            Total = total,
            Items = items.Select(ToDto).ToList()
        };
    }

    public async Task<List<string>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        return await _configRepository.GetQueryable()
            .Where(x => x.Group != null && x.Group != string.Empty)
            .Select(x => x.Group!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    public async Task<ConfigDto> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        return ToDto(entity);
    }

    public async Task<ConfigDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new RbacException("配置键不能为空");
        }

        var entity = await _configRepository.GetQueryable()
            .FirstOrDefaultAsync(x => x.Key == key.Trim(), cancellationToken);

        return entity == null ? null : ToDto(entity);
    }

    public async Task<ConfigDto> CreateAsync(
        CreateConfigDto input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Key) ||
            string.IsNullOrWhiteSpace(input.Value) ||
            string.IsNullOrWhiteSpace(input.Label))
        {
            throw new RbacException("配置键、标签和值不能为空");
        }

        var key = input.Key.Trim();
        if (await _configRepository.AnyAsync(x => x.Key == key, cancellationToken))
        {
            throw new RbacException("配置键已存在");
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
        return ToDto(entity);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        await _configRepository.DeleteAsync(id, cancellationToken: cancellationToken);
    }

    private async Task<SysConfig> FindAsync(long id, CancellationToken cancellationToken)
    {
        var entity = await _configRepository.FindAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            throw new RbacException($"配置不存在: {id}");
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
