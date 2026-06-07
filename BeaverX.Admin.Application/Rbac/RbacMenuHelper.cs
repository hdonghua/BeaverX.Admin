using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared.Rbac;

namespace BeaverX.Admin.Application.Rbac;

internal static class RbacMenuHelper
{
    public static List<string> CollectPerms(IEnumerable<Menu> menus)
    {
        return menus
            .Where(m => m.IsEnabled && !string.IsNullOrWhiteSpace(m.Perms))
            .Select(m => m.Perms!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<Menu> FilterRouters(IEnumerable<Menu> menus, HashSet<long> roleMenuIds, bool isSuperAdmin)
    {
        var list = menus
            .Where(m => m.IsEnabled)
            .Where(m => m.MenuType is MenuType.Directory or MenuType.Menu)
            .ToList();

        if (isSuperAdmin)
        {
            return list;
        }

        if (roleMenuIds.Count == 0)
        {
            return [];
        }

        var idMap = menus.ToDictionary(m => m.Id);
        var allowedIds = new HashSet<long>(roleMenuIds);
        foreach (var menuId in roleMenuIds)
        {
            IncludeAncestors(menuId, idMap, allowedIds);
        }

        return list.Where(m => allowedIds.Contains(m.Id)).ToList();
    }

    private static void IncludeAncestors(long menuId, IReadOnlyDictionary<long, Menu> idMap, ISet<long> allowedIds)
    {
        if (!idMap.TryGetValue(menuId, out var current))
        {
            return;
        }

        allowedIds.Add(current.Id);
        if (current.ParentId.HasValue)
        {
            IncludeAncestors(current.ParentId.Value, idMap, allowedIds);
        }
    }

    public static List<MenuDto> ToRouterTree(IEnumerable<MenuDto> items, long? parentId = null)
    {
        return items
            .Where(x => x.ParentId == parentId)
            .OrderBy(x => x.Sort)
            .Select(x =>
            {
                x.Children = ToRouterTree(items, x.Id);
                return x;
            })
            .ToList();
    }
}
