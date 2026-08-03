using BeaverX.Admin.Application.Caching;
using BeaverX.Admin.Application.Contracts.Caching;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Rbac;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace BeaverX.Admin.Application.Rbac;

public class MenuCacheService : IScopedDependency
{
    private readonly IRepository<Menu, Guid> _menuRepository;
    private readonly ICacheService _cache;

    public MenuCacheService(IRepository<Menu, Guid> menuRepository, ICacheService cache)
    {
        _menuRepository = menuRepository;
        _cache = cache;
    }

    public async Task<List<Menu>> GetAllMenusAsync(CancellationToken cancellationToken = default)
    {
        var items = await _cache.GetOrSetAsync(
            CacheKeys.MenuAll,
            async ct =>
            {
                var menus = await _menuRepository.GetListAsync(cancellationToken: ct);
                return menus.Select(MenuCacheItem.FromEntity).ToList();
            },
            CacheDurations.Menu,
            cancellationToken);

        return items.Select(x => x.ToEntity()).ToList();
    }

    public Task<List<MenuDto>> GetMenuTreeAsync(CancellationToken cancellationToken = default) =>
        _cache.GetOrSetAsync(
            CacheKeys.MenuTree,
            async ct =>
            {
                var menus = await _menuRepository.GetListAsync(cancellationToken: ct);
                var dtos = menus.Select(RbacMapper.ToMenuDto).ToList();
                return RbacQueryHelper.BuildMenuTree(dtos) ?? [];
            },
            CacheDurations.Menu,
            cancellationToken);
}
