using BeaverX.Admin.Domain.DataSeeder;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Config;

public class ConfigMenuSeeder : IScopedDependency, IDataSeeder
{
    private readonly IRepository<Menu> _menuRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<RoleMenu> _roleMenuRepository;
    private readonly ILogger<ConfigMenuSeeder> _logger;

    public ConfigMenuSeeder(
        IRepository<Menu> menuRepository,
        IRepository<Role> roleRepository,
        IRepository<RoleMenu> roleMenuRepository,
        ILogger<ConfigMenuSeeder> logger)
    {
        _menuRepository = menuRepository;
        _roleRepository = roleRepository;
        _roleMenuRepository = roleMenuRepository;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _menuRepository.AnyAsync(
                x => x.Perms == RbacPermissionCodes.System.Config.List,
                cancellationToken))
        {
            return;
        }

        var systemDir = await _menuRepository.GetQueryable()
            .FirstOrDefaultAsync(x => x.Path == "/system" && x.MenuType == MenuType.Directory, cancellationToken);

        if (systemDir == null)
        {
            _logger.LogWarning("Config menu seed skipped: system directory menu not found.");
            return;
        }

        _logger.LogInformation("Seeding config menus...");

        var page = new Menu
        {
            ParentId = systemDir.Id,
            Name = "配置管理",
            MenuType = MenuType.Menu,
            Perms = RbacPermissionCodes.System.Config.List,
            Path = "/system/config",
            Component = "system/config/index",
            Icon = "settings",
            Sort = 5,
            IsVisible = true
        };
        await _menuRepository.InsertAsync(page, cancellationToken: cancellationToken);

        var buttons = new[]
        {
            Btn(page.Id, "配置新增", RbacPermissionCodes.System.Config.Create, 1),
            Btn(page.Id, "配置修改", RbacPermissionCodes.System.Config.Update, 2),
            Btn(page.Id, "配置删除", RbacPermissionCodes.System.Config.Delete, 3),
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
