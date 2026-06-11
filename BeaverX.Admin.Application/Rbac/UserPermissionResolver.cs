using BeaverX.Admin.Application.Caching;
using BeaverX.Admin.Application.Contracts.Caching;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Rbac;

public class UserPermissionResolver : IUserPermissionResolver, IScopedDependency
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Menu> _menuRepository;
    private readonly MenuCacheService _menuCacheService;
    private readonly ICacheService _cache;
    private readonly AppCacheInvalidator _cacheInvalidator;

    public UserPermissionResolver(
        IRepository<User> userRepository,
        IRepository<Menu> menuRepository,
        MenuCacheService menuCacheService,
        ICacheService cache,
        AppCacheInvalidator cacheInvalidator)
    {
        _userRepository = userRepository;
        _menuRepository = menuRepository;
        _menuCacheService = menuCacheService;
        _cache = cache;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        var accessVersion = await _cacheInvalidator.GetAccessVersionAsync(cancellationToken);
        var cacheKey = CacheKeys.UserPermissions(userId, accessVersion);

        return await _cache.GetOrSetAsync(
            cacheKey,
            ct => LoadPermissionsFromDatabaseAsync(userId, ct),
            CacheDurations.UserAccess,
            cancellationToken);
    }

    private async Task<List<string>> LoadPermissionsFromDatabaseAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetQueryable()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .ThenInclude(x => x.RoleMenus)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user == null || !user.IsEnabled)
        {
            return [];
        }

        return await ResolvePermissionsCoreAsync(GetRoleCodes(user), user, cancellationToken);
    }

    private async Task<List<string>> ResolvePermissionsCoreAsync(
        List<string> roles,
        User user,
        CancellationToken cancellationToken)
    {
        if (IsSuperAdmin(roles))
        {
            var allMenus = await _menuCacheService.GetAllMenusAsync(cancellationToken);
            return RbacMenuHelper.CollectPerms(allMenus);
        }

        var roleMenuIds = user.UserRoles
            .SelectMany(x => x.Role.RoleMenus)
            .Select(x => x.MenuId)
            .ToHashSet();

        if (roleMenuIds.Count == 0)
        {
            return [];
        }

        var menus = await _menuRepository.GetListAsync(x => roleMenuIds.Contains(x.Id), cancellationToken);
        return RbacMenuHelper.CollectPerms(menus);
    }

    private static List<string> GetRoleCodes(User user) =>
        user.UserRoles
            .Where(x => x.Role.IsEnabled)
            .Select(x => x.Role.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static bool IsSuperAdmin(IEnumerable<string> roles) =>
        roles.Contains(RbacPermissionCodes.SuperAdmin, StringComparer.OrdinalIgnoreCase);
}
