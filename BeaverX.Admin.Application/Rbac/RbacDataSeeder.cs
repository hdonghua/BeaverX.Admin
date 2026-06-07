using BeaverX.Admin.Domain.DataSeeder;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Rbac;

public class RbacDataSeeder : IScopedDependency, IDataSeeder
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<Menu> _menuRepository;
    private readonly IRepository<UserRole> _userRoleRepository;
    private readonly IRepository<RoleMenu> _roleMenuRepository;
    private readonly ILogger<RbacDataSeeder> _logger;

    public RbacDataSeeder(
        IRepository<User> userRepository,
        IRepository<Role> roleRepository,
        IRepository<Menu> menuRepository,
        IRepository<UserRole> userRoleRepository,
        IRepository<RoleMenu> roleMenuRepository,
        ILogger<RbacDataSeeder> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _menuRepository = menuRepository;
        _userRoleRepository = userRoleRepository;
        _roleMenuRepository = roleMenuRepository;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _userRepository.AnyAsync(_ => true, cancellationToken))
        {
            return;
        }

        _logger.LogInformation("Seeding RBAC initial data...");

        var allMenus = new List<Menu>();

        var systemDir = await InsertMenuAsync(new Menu
        {
            Name = "系统管理",
            MenuType = MenuType.Directory,
            Path = "/system",
            Icon = "setting",
            Sort = 1,
            IsVisible = true
        }, cancellationToken);
        allMenus.Add(systemDir);

        allMenus.AddRange(await SeedUserMenusAsync(systemDir.Id, cancellationToken));
        allMenus.AddRange(await SeedRoleMenusAsync(systemDir.Id, cancellationToken));
        allMenus.AddRange(await SeedMenuMenusAsync(systemDir.Id, cancellationToken));
        allMenus.AddRange(await SeedDictMenusAsync(systemDir.Id, cancellationToken));

        var adminRole = new Role
        {
            Code = RbacPermissionCodes.SuperAdmin,
            Name = "超级管理员",
            Description = "拥有系统全部权限",
            Sort = 0,
            IsEnabled = true
        };
        await _roleRepository.InsertAsync(adminRole, cancellationToken: cancellationToken);

        await _roleMenuRepository.InsertManyAsync(
            allMenus.Select(menu => new RoleMenu { RoleId = adminRole.Id, MenuId = menu.Id }),
            cancellationToken: cancellationToken);

        var adminUser = new User
        {
            UserName = "admin",
            PasswordHash = PasswordHasher.Hash("admin123"),
            NickName = "系统管理员",
            IsEnabled = true
        };
        await _userRepository.InsertAsync(adminUser, cancellationToken: cancellationToken);

        await _userRoleRepository.InsertAsync(new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        }, cancellationToken: cancellationToken);

        _logger.LogInformation("RBAC initial data seeded. Default account: admin / admin123");
    }

    private async Task<List<Menu>> SeedUserMenusAsync(long parentId, CancellationToken cancellationToken)
    {
        var page = await InsertMenuAsync(new Menu
        {
            ParentId = parentId,
            Name = "用户管理",
            MenuType = MenuType.Menu,
            Perms = RbacPermissionCodes.System.User.List,
            Path = "/system/user",
            Component = "system/user/index",
            Icon = "user",
            Sort = 1
        }, cancellationToken);

        var buttons = await InsertManyAsync([
            Btn(page.Id, "用户新增", RbacPermissionCodes.System.User.Create, 1),
            Btn(page.Id, "用户修改", RbacPermissionCodes.System.User.Update, 2),
            Btn(page.Id, "用户删除", RbacPermissionCodes.System.User.Delete, 3),
            Btn(page.Id, "分配角色", RbacPermissionCodes.System.User.AssignRoles, 4),
            Btn(page.Id, "重置密码", RbacPermissionCodes.System.User.ResetPassword, 5),
        ], cancellationToken);

        var result = new List<Menu> { page };
        result.AddRange(buttons);
        return result;
    }

    private async Task<List<Menu>> SeedRoleMenusAsync(long parentId, CancellationToken cancellationToken)
    {
        var page = await InsertMenuAsync(new Menu
        {
            ParentId = parentId,
            Name = "角色管理",
            MenuType = MenuType.Menu,
            Perms = RbacPermissionCodes.System.Role.List,
            Path = "/system/role",
            Component = "system/role/index",
            Icon = "team",
            Sort = 2
        }, cancellationToken);

        var buttons = await InsertManyAsync([
            Btn(page.Id, "角色新增", RbacPermissionCodes.System.Role.Create, 1),
            Btn(page.Id, "角色修改", RbacPermissionCodes.System.Role.Update, 2),
            Btn(page.Id, "角色删除", RbacPermissionCodes.System.Role.Delete, 3),
            Btn(page.Id, "分配菜单", RbacPermissionCodes.System.Role.AssignMenus, 4),
        ], cancellationToken);

        var result = new List<Menu> { page };
        result.AddRange(buttons);
        return result;
    }

    private async Task<List<Menu>> SeedMenuMenusAsync(long parentId, CancellationToken cancellationToken)
    {
        var page = await InsertMenuAsync(new Menu
        {
            ParentId = parentId,
            Name = "菜单管理",
            MenuType = MenuType.Menu,
            Perms = RbacPermissionCodes.System.Menu.List,
            Path = "/system/menu",
            Component = "system/menu/index",
            Icon = "menu",
            Sort = 3
        }, cancellationToken);

        var buttons = await InsertManyAsync([
            Btn(page.Id, "菜单新增", RbacPermissionCodes.System.Menu.Create, 1),
            Btn(page.Id, "菜单修改", RbacPermissionCodes.System.Menu.Update, 2),
            Btn(page.Id, "菜单删除", RbacPermissionCodes.System.Menu.Delete, 3),
        ], cancellationToken);

        var result = new List<Menu> { page };
        result.AddRange(buttons);
        return result;
    }

    private async Task<List<Menu>> SeedDictMenusAsync(long parentId, CancellationToken cancellationToken)
    {
        var page = await InsertMenuAsync(new Menu
        {
            ParentId = parentId,
            Name = "字典管理",
            MenuType = MenuType.Menu,
            Perms = RbacPermissionCodes.System.Dict.List,
            Path = "/system/dict",
            Component = "system/dict/index",
            Icon = "book",
            Sort = 4
        }, cancellationToken);

        var buttons = await InsertManyAsync([
            Btn(page.Id, "字典类型新增", RbacPermissionCodes.System.Dict.Type.Create, 1),
            Btn(page.Id, "字典类型修改", RbacPermissionCodes.System.Dict.Type.Update, 2),
            Btn(page.Id, "字典类型删除", RbacPermissionCodes.System.Dict.Type.Delete, 3),
            Btn(page.Id, "字典数据新增", RbacPermissionCodes.System.Dict.Data.Create, 4),
            Btn(page.Id, "字典数据修改", RbacPermissionCodes.System.Dict.Data.Update, 5),
            Btn(page.Id, "字典数据删除", RbacPermissionCodes.System.Dict.Data.Delete, 6),
        ], cancellationToken);

        var result = new List<Menu> { page };
        result.AddRange(buttons);
        return result;
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

    private async Task<Menu> InsertMenuAsync(Menu menu, CancellationToken cancellationToken)
    {
        await _menuRepository.InsertAsync(menu, cancellationToken: cancellationToken);
        return menu;
    }

    private async Task<List<Menu>> InsertManyAsync(IEnumerable<Menu> menus, CancellationToken cancellationToken)
    {
        var list = menus.ToList();
        await _menuRepository.InsertManyAsync(list, cancellationToken: cancellationToken);
        return list;
    }
}
