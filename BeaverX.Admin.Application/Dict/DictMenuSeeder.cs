using BeaverX.Admin.Domain.DataSeeder;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Dict;

public class DictMenuSeeder : IScopedDependency, IDataSeeder
{
    private readonly IRepository<Menu> _menuRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<RoleMenu> _roleMenuRepository;
    private readonly ILogger<DictMenuSeeder> _logger;

    public DictMenuSeeder(
        IRepository<Menu> menuRepository,
        IRepository<Role> roleRepository,
        IRepository<RoleMenu> roleMenuRepository,
        ILogger<DictMenuSeeder> logger)
    {
        _menuRepository = menuRepository;
        _roleRepository = roleRepository;
        _roleMenuRepository = roleMenuRepository;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _menuRepository.AnyAsync(
                x => x.Perms == RbacPermissionCodes.System.Dict.List,
                cancellationToken))
        {
            return;
        }

        var systemDir = await _menuRepository.GetQueryable()
            .FirstOrDefaultAsync(x => x.Path == "/system" && x.MenuType == MenuType.Directory, cancellationToken);

        if (systemDir == null)
        {
            _logger.LogWarning("Dict menu seed skipped: system directory menu not found.");
            return;
        }

        _logger.LogInformation("Seeding dictionary menus...");

        var page = new Menu
        {
            ParentId = systemDir.Id,
            Name = "字典管理",
            MenuType = MenuType.Menu,
            Perms = RbacPermissionCodes.System.Dict.List,
            Path = "/system/dict",
            Component = "system/dict/index",
            Icon = "book",
            Sort = 4,
            IsVisible = true
        };
        await _menuRepository.InsertAsync(page, cancellationToken: cancellationToken);

        var buttons = new[]
        {
            Btn(page.Id, "字典类型新增", RbacPermissionCodes.System.Dict.Type.Create, 1),
            Btn(page.Id, "字典类型修改", RbacPermissionCodes.System.Dict.Type.Update, 2),
            Btn(page.Id, "字典类型删除", RbacPermissionCodes.System.Dict.Type.Delete, 3),
            Btn(page.Id, "字典数据新增", RbacPermissionCodes.System.Dict.Data.Create, 4),
            Btn(page.Id, "字典数据修改", RbacPermissionCodes.System.Dict.Data.Update, 5),
            Btn(page.Id, "字典数据删除", RbacPermissionCodes.System.Dict.Data.Delete, 6),
        };
        await _menuRepository.InsertManyAsync(buttons, cancellationToken: cancellationToken);

        var adminRole = await _roleRepository.GetQueryable()
            .FirstOrDefaultAsync(x => x.Code == RbacPermissionCodes.SuperAdmin, cancellationToken);

        if (adminRole != null)
        {
            var menuIds = new[] { page.Id }.Concat(buttons.Select(x => x.Id));
            await _roleMenuRepository.InsertManyAsync(
                menuIds.Select(menuId => new RoleMenu { RoleId = adminRole.Id, MenuId = menuId }),
                cancellationToken: cancellationToken);
        }
    }

    private static Menu Btn(long parentId, string name, string perms, int sort) => new()
    {
        ParentId = parentId,
        Name = name,
        MenuType = MenuType.Button,
        Perms = perms,
        Sort = sort,
        IsVisible = false
    };
}
