using BeaverX.Admin.Application.Caching;
using BeaverX.Admin.Application.Contracts.Caching;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;

namespace BeaverX.Admin.Application.Rbac;

public class MenuCacheService : IScopedDependency
{
    private readonly IRepository<Menu> _menuRepository;
    private readonly ICacheService _cache;

    public MenuCacheService(IRepository<Menu> menuRepository, ICacheService cache)
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
                var menus = await _menuRepository.GetListAsync(ct);
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
                var menus = await _menuRepository.GetListAsync(ct);
                var dtos = menus.Select(RbacMapper.ToMenuDto).ToList();
                return RbacQueryHelper.BuildMenuTree(dtos);
            },
            CacheDurations.Menu,
            cancellationToken);
}
