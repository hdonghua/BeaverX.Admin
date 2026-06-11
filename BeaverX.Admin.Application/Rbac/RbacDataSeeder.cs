using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Domain.DataSeeder;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Rbac;

public class RbacDataSeeder : IScopedDependency, IDataSeeder
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<Menu> _menuRepository;
    private readonly IRepository<UserRole> _userRoleRepository;
    private readonly IRepository<RoleMenu> _roleMenuRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<RbacDataSeeder> _logger;

    public RbacDataSeeder(
        IRepository<User> userRepository,
        IRepository<Role> roleRepository,
        IRepository<Menu> menuRepository,
        IRepository<UserRole> userRoleRepository,
        IRepository<RoleMenu> roleMenuRepository,
        IPasswordHasher passwordHasher,
        ILogger<RbacDataSeeder> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _menuRepository = menuRepository;
        _userRoleRepository = userRoleRepository;
        _roleMenuRepository = roleMenuRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var newMenus = await EnsureAllMenusAsync(cancellationToken);
        var adminRole = await EnsureSuperAdminRoleAsync(newMenus, cancellationToken);
        await EnsureAdminUserAsync(adminRole, cancellationToken);
    }

    private async Task<Role> EnsureSuperAdminRoleAsync(
        List<Menu> newMenus,
        CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetQueryable()
            .FirstOrDefaultAsync(x => x.Code == RbacPermissionCodes.SuperAdmin, cancellationToken);

        if (role == null)
        {
            _logger.LogInformation("Seeding super admin role...");

            role = new Role
            {
                Code = RbacPermissionCodes.SuperAdmin,
                Name = "超级管理员",
                Description = "拥有系统全部权限",
                Sort = 0,
                IsEnabled = true
            };
            await _roleRepository.InsertAsync(role, cancellationToken: cancellationToken);

            var allMenus = await _menuRepository.GetListAsync(_ => true, cancellationToken);
            await InsertRoleMenusIfMissingAsync(role.Id, allMenus, cancellationToken);
            return role;
        }

        if (newMenus.Count > 0)
        {
            await InsertRoleMenusIfMissingAsync(role.Id, newMenus, cancellationToken);
        }

        return role;
    }

    private async Task EnsureAdminUserAsync(Role adminRole, CancellationToken cancellationToken)
    {
        var adminUser = await _userRepository.GetQueryable()
            .FirstOrDefaultAsync(x => x.UserName == "admin", cancellationToken);

        if (adminUser == null)
        {
            _logger.LogInformation("Seeding default admin user...");

            adminUser = new User
            {
                UserName = "admin",
                PasswordHash = _passwordHasher.Hash("admin123"),
                NickName = "系统管理员",
                IsEnabled = true
            };
            await _userRepository.InsertAsync(adminUser, cancellationToken: cancellationToken);
        }

        if (!await _userRoleRepository.AnyAsync(
                x => x.UserId == adminUser.Id && x.RoleId == adminRole.Id,
                cancellationToken))
        {
            await _userRoleRepository.InsertAsync(new UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            }, cancellationToken: cancellationToken);
        }
    }

    private async Task InsertRoleMenusIfMissingAsync(
        long roleId,
        IEnumerable<Menu> menus,
        CancellationToken cancellationToken)
    {
        var menuList = menus.ToList();
        if (menuList.Count == 0)
        {
            return;
        }

        var menuIds = menuList.Select(x => x.Id).ToList();
        var existingMenuIds = await _roleMenuRepository.GetQueryable()
            .Where(x => x.RoleId == roleId && menuIds.Contains(x.MenuId))
            .Select(x => x.MenuId)
            .ToListAsync(cancellationToken);

        var missing = menuList
            .Where(menu => !existingMenuIds.Contains(menu.Id))
            .Select(menu => new RoleMenu { RoleId = roleId, MenuId = menu.Id })
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        await _roleMenuRepository.InsertManyAsync(missing, cancellationToken: cancellationToken);
    }

    private async Task<List<Menu>> EnsureAllMenusAsync(CancellationToken cancellationToken)
    {
        var newMenus = new List<Menu>();

        var (systemDir, systemDirMenus) = await EnsureSystemDirectoryAsync(cancellationToken);
        newMenus.AddRange(systemDirMenus);

        newMenus.AddRange(await EnsureUserMenusAsync(systemDir.Id, cancellationToken));
        newMenus.AddRange(await EnsureRoleMenusAsync(systemDir.Id, cancellationToken));
        newMenus.AddRange(await EnsureMenuMenusAsync(systemDir.Id, cancellationToken));
        newMenus.AddRange(await EnsureDictMenusAsync(systemDir.Id, cancellationToken));
        newMenus.AddRange(await EnsureConfigMenusAsync(systemDir.Id, cancellationToken));
        newMenus.AddRange(await EnsureMessageMenusAsync(systemDir.Id, cancellationToken));
        newMenus.AddRange(await EnsurePaymentMenusAsync(cancellationToken));

        return newMenus;
    }

    private async Task<(Menu Directory, List<Menu> NewMenus)> EnsureSystemDirectoryAsync(
        CancellationToken cancellationToken)
    {
        var existing = await _menuRepository.GetQueryable()
            .FirstOrDefaultAsync(
                x => x.Path == "/system" && x.MenuType == MenuType.Directory,
                cancellationToken);

        if (existing != null)
        {
            return (existing, []);
        }

        _logger.LogInformation("Seeding system directory menu...");

        var systemDir = await InsertMenuAsync(new Menu
        {
            Name = "系统管理",
            MenuType = MenuType.Directory,
            Path = "/system",
            Icon = "setting",
            Sort = 1,
            IsVisible = true
        }, cancellationToken);

        return (systemDir, [systemDir]);
    }

    private async Task<List<Menu>> EnsureUserMenusAsync(long parentId, CancellationToken cancellationToken)
    {
        if (await _menuRepository.AnyAsync(
                x => x.Perms == RbacPermissionCodes.System.User.List,
                cancellationToken))
        {
            return [];
        }

        _logger.LogInformation("Seeding user menus...");

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
            //Btn(page.Id, "用户修改", RbacPermissionCodes.System.User.Update, 2),
            //Btn(page.Id, "用户删除", RbacPermissionCodes.System.User.Delete, 3),
            Btn(page.Id, "分配角色", RbacPermissionCodes.System.User.AssignRoles, 2),
            Btn(page.Id, "重置密码", RbacPermissionCodes.System.User.ResetPassword, 3),
        ], cancellationToken);

        return [page, ..buttons];
    }

    private async Task<List<Menu>> EnsureRoleMenusAsync(long parentId, CancellationToken cancellationToken)
    {
        if (await _menuRepository.AnyAsync(
                x => x.Perms == RbacPermissionCodes.System.Role.List,
                cancellationToken))
        {
            return [];
        }

        _logger.LogInformation("Seeding role menus...");

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

        return [page, ..buttons];
    }

    private async Task<List<Menu>> EnsureMenuMenusAsync(long parentId, CancellationToken cancellationToken)
    {
        if (await _menuRepository.AnyAsync(
                x => x.Perms == RbacPermissionCodes.System.Menu.List,
                cancellationToken))
        {
            return [];
        }

        _logger.LogInformation("Seeding menu management menus...");

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

        return [page, ..buttons];
    }

    private async Task<List<Menu>> EnsureDictMenusAsync(long parentId, CancellationToken cancellationToken)
    {
        if (await _menuRepository.AnyAsync(
                x => x.Perms == RbacPermissionCodes.System.Dict.List,
                cancellationToken))
        {
            return [];
        }

        _logger.LogInformation("Seeding dictionary menus...");

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

        return [page, ..buttons];
    }

    private async Task<List<Menu>> EnsureConfigMenusAsync(long parentId, CancellationToken cancellationToken)
    {
        if (await _menuRepository.AnyAsync(
                x => x.Perms == RbacPermissionCodes.System.Config.List,
                cancellationToken))
        {
            return [];
        }

        _logger.LogInformation("Seeding config menus...");

        var page = await InsertMenuAsync(new Menu
        {
            ParentId = parentId,
            Name = "配置管理",
            MenuType = MenuType.Menu,
            Perms = RbacPermissionCodes.System.Config.List,
            Path = "/system/config",
            Component = "system/config/index",
            Icon = "settings",
            Sort = 5
        }, cancellationToken);

        var buttons = await InsertManyAsync([
            Btn(page.Id, "配置新增", RbacPermissionCodes.System.Config.Create, 1),
            Btn(page.Id, "配置修改", RbacPermissionCodes.System.Config.Update, 2),
            Btn(page.Id, "配置删除", RbacPermissionCodes.System.Config.Delete, 3),
        ], cancellationToken);

        return [page, ..buttons];
    }

    private async Task<List<Menu>> EnsureMessageMenusAsync(long parentId, CancellationToken cancellationToken)
    {
        if (await _menuRepository.AnyAsync(
                x => x.Perms == RbacPermissionCodes.System.Message.Send,
                cancellationToken))
        {
            return [];
        }

        _logger.LogInformation("Seeding site message menus...");

        var page = await InsertMenuAsync(new Menu
        {
            ParentId = parentId,
            Name = "发送站内信",
            MenuType = MenuType.Menu,
            Perms = RbacPermissionCodes.System.Message.Send,
            Path = "/system/message",
            Component = "system/message/send",
            Icon = "message",
            Sort = 6
        }, cancellationToken);

        return [page];
    }

    private async Task<List<Menu>> EnsurePaymentMenusAsync(CancellationToken cancellationToken)
    {
        var newMenus = new List<Menu>();

        var (paymentDir, dirMenus) = await EnsurePaymentDirectoryAsync(cancellationToken);
        newMenus.AddRange(dirMenus);

        newMenus.AddRange(await EnsurePaymentChannelMenusAsync(paymentDir.Id, cancellationToken));
        newMenus.AddRange(await EnsurePaymentOrderMenusAsync(paymentDir.Id, cancellationToken));
        newMenus.AddRange(await EnsurePaymentRefundMenusAsync(paymentDir.Id, cancellationToken));

        return newMenus;
    }

    private async Task<(Menu Directory, List<Menu> NewMenus)> EnsurePaymentDirectoryAsync(
        CancellationToken cancellationToken)
    {
        var existing = await _menuRepository.GetQueryable()
            .FirstOrDefaultAsync(
                x => x.Path == "/payment" && x.MenuType == MenuType.Directory,
                cancellationToken);

        if (existing != null)
        {
            return (existing, []);
        }

        _logger.LogInformation("Seeding payment directory menu...");

        var paymentDir = await InsertMenuAsync(new Menu
        {
            Name = "支付管理",
            MenuType = MenuType.Directory,
            Path = "/payment",
            Icon = "alipay-circle",
            Sort = 20,
            IsVisible = true
        }, cancellationToken);

        return (paymentDir, [paymentDir]);
    }

    private async Task<List<Menu>> EnsurePaymentChannelMenusAsync(
        long parentId,
        CancellationToken cancellationToken)
    {
        if (await _menuRepository.AnyAsync(
                x => x.Perms == RbacPermissionCodes.Payment.Channel.List,
                cancellationToken))
        {
            return [];
        }

        _logger.LogInformation("Seeding payment channel menus...");

        var channelPage = await InsertMenuAsync(new Menu
        {
            ParentId = parentId,
            Name = "支付渠道",
            MenuType = MenuType.Menu,
            Perms = RbacPermissionCodes.Payment.Channel.List,
            Path = "/payment/channel",
            Component = "payment/channel/index",
            Icon = "settings",
            Sort = 1
        }, cancellationToken);

        var buttons = await InsertManyAsync([
            Btn(channelPage.Id, "渠道新增", RbacPermissionCodes.Payment.Channel.Create, 1),
            Btn(channelPage.Id, "渠道修改", RbacPermissionCodes.Payment.Channel.Update, 2),
            Btn(channelPage.Id, "渠道删除", RbacPermissionCodes.Payment.Channel.Delete, 3),
        ], cancellationToken);

        return [channelPage, ..buttons];
    }

    private async Task<List<Menu>> EnsurePaymentOrderMenusAsync(
        long parentId,
        CancellationToken cancellationToken)
    {
        if (await _menuRepository.AnyAsync(
                x => x.Perms == RbacPermissionCodes.Payment.Order.List,
                cancellationToken))
        {
            return [];
        }

        _logger.LogInformation("Seeding payment order menus...");

        var orderPage = await InsertMenuAsync(new Menu
        {
            ParentId = parentId,
            Name = "支付订单",
            MenuType = MenuType.Menu,
            Perms = RbacPermissionCodes.Payment.Order.List,
            Path = "/payment/order",
            Component = "payment/order/index",
            Icon = "ordered-list",
            Sort = 2
        }, cancellationToken);

        var buttons = await InsertManyAsync([
            Btn(orderPage.Id, "创建订单", RbacPermissionCodes.Payment.Order.Create, 1),
            Btn(orderPage.Id, "查询订单", RbacPermissionCodes.Payment.Order.Query, 2),
            Btn(orderPage.Id, "关闭订单", RbacPermissionCodes.Payment.Order.Close, 3),
            Btn(orderPage.Id, "订单退款", RbacPermissionCodes.Payment.Order.Refund, 4),
        ], cancellationToken);

        return [orderPage, ..buttons];
    }

    private async Task<List<Menu>> EnsurePaymentRefundMenusAsync(
        long parentId,
        CancellationToken cancellationToken)
    {
        if (await _menuRepository.AnyAsync(
                x => x.Perms == RbacPermissionCodes.Payment.Refund.List,
                cancellationToken))
        {
            return [];
        }

        _logger.LogInformation("Seeding payment refund menus...");

        var refundPage = await InsertMenuAsync(new Menu
        {
            ParentId = parentId,
            Name = "退款记录",
            MenuType = MenuType.Menu,
            Perms = RbacPermissionCodes.Payment.Refund.List,
            Path = "/payment/refund",
            Component = "payment/refund/index",
            Icon = "swap",
            Sort = 3
        }, cancellationToken);

        return [refundPage];
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
