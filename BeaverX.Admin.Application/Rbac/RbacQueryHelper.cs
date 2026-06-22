using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Rbac;

internal static class RbacQueryHelper
{
    public static (int Skip, int Take) GetPaging(int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);
        return ((page - 1) * pageSize, pageSize);
    }

    public static List<MenuDto>? BuildMenuTree(IEnumerable<MenuDto> items, long? parentId = null)
    {
        var list = items
            .Where(x => x.ParentId == parentId)
            .OrderBy(x => x.Sort)
            .Select(x =>
            {
                x.Children = BuildMenuTree(items, x.Id);
                return x;
            })
            .ToList();
        return list.Count == 0 ? null : list;
    }
}
