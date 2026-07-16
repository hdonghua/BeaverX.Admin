using BeaverX.Admin.Application.Caching;
using BeaverX.Admin.Application.Contracts.Caching;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;

namespace BeaverX.Admin.Application.Rbac;

public class UserPermissionResolver : IUserPermissionResolver, IScopedDependency
{
    private readonly ISugarRepository<User> _userRepository;
    private readonly ISugarRepository<Menu> _menuRepository;
    private readonly MenuCacheService _menuCacheService;
    private readonly ICacheService _cache;
    private readonly AppCacheInvalidator _cacheInvalidator;

    public UserPermissionResolver(
        ISugarRepository<User> userRepository,
        ISugarRepository<Menu> menuRepository,
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
        var user = await _userRepository.FindAsync(x => x.Id == userId, cancellationToken);

        if (user == null || !user.IsEnabled)
        {
            return [];
        }

        await PopulateUserAccessAsync(user, cancellationToken);
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

        var menus = await _menuRepository.GetSugarQueryable()
            .Where(x => roleMenuIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
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

    private async Task PopulateUserAccessAsync(User user, CancellationToken cancellationToken)
    {
        var userRoles = await _userRepository.Client.Queryable<UserRole>()
            .Where(x => x.UserId == user.Id)
            .ToListAsync(cancellationToken);

        var roleIds = userRoles.Select(x => x.RoleId).Distinct().ToList();
        if (roleIds.Count == 0)
        {
            user.UserRoles = [];
            return;
        }

        var roles = await _userRepository.Client.Queryable<Role>()
            .Where(x => roleIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        var roleMap = roles.ToDictionary(x => x.Id);

        var roleMenus = await _userRepository.Client.Queryable<RoleMenu>()
            .Where(x => roleIds.Contains(x.RoleId))
            .ToListAsync(cancellationToken);
        var roleMenuMap = roleMenus
            .GroupBy(x => x.RoleId)
            .ToDictionary(x => x.Key, x => (ICollection<RoleMenu>)x.ToList());

        foreach (var role in roles)
        {
            role.RoleMenus = roleMenuMap.TryGetValue(role.Id, out var items) ? items : [];
        }

        foreach (var userRole in userRoles)
        {
            if (roleMap.TryGetValue(userRole.RoleId, out var role))
            {
                userRole.Role = role;
            }
        }

        user.UserRoles = userRoles;
    }
}
