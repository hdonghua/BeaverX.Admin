using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Demo;
using BeaverX.Admin.Domain.DataSeeder;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Rbac;

public class RbacDataSeeder : IScopedDependency, IDataSeeder, IOverwriteDataSeeder
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<Menu> _menuRepository;
    private readonly IRepository<UserRole> _userRoleRepository;
    private readonly IRepository<RoleMenu> _roleMenuRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDemoDatabaseHardResetService _demoHardResetService;
    private readonly ILogger<RbacDataSeeder> _logger;

    public RbacDataSeeder(
        IRepository<User> userRepository,
        IRepository<Role> roleRepository,
        IRepository<Menu> menuRepository,
        IRepository<UserRole> userRoleRepository,
        IRepository<RoleMenu> roleMenuRepository,
        IPasswordHasher passwordHasher,
        IDemoDatabaseHardResetService demoHardResetService,
        ILogger<RbacDataSeeder> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _menuRepository = menuRepository;
        _userRoleRepository = userRoleRepository;
        _roleMenuRepository = roleMenuRepository;
        _passwordHasher = passwordHasher;
        _demoHardResetService = demoHardResetService;
        _logger = logger;
    }

    public int Order => 10;

    public async Task OverwriteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Overwriting RBAC demo data...");

        await _demoHardResetService.ClearMenusAsync(cancellationToken);
        await _demoHardResetService.ClearNonAdminUsersAsync(cancellationToken);
        await SeedAsync(cancellationToken);
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var newMenuIds = await EnsureAllMenusAsync(cancellationToken);
        var adminRole = await EnsureSuperAdminRoleAsync(newMenuIds, cancellationToken);
        await EnsureAdminUserAsync(adminRole, cancellationToken);
    }

    private async Task<Role> EnsureSuperAdminRoleAsync(
        List<long> newMenuIds,
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

            var allMenuIds = await _menuRepository.GetQueryable()
                .AsNoTracking()
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            await InsertRoleMenusIfMissingAsync(role.Id, allMenuIds, cancellationToken);
            return role;
        }

        if (newMenuIds.Count > 0)
        {
            await InsertRoleMenusIfMissingAsync(role.Id, newMenuIds, cancellationToken);
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
                PasswordHash = _passwordHasher.Hash("Admin@123"),
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
        IEnumerable<long> menuIds,
        CancellationToken cancellationToken)
    {
        var menuIdList = menuIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        if (menuIdList.Count == 0)
        {
            return;
        }

        var existingMenuIds = await _roleMenuRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => x.RoleId == roleId && menuIdList.Contains(x.MenuId))
            .Select(x => x.MenuId)
            .ToListAsync(cancellationToken);

        var missing = menuIdList
            .Where(menuId => !existingMenuIds.Contains(menuId))
            .Select(menuId => new RoleMenu { RoleId = roleId, MenuId = menuId })
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        await _roleMenuRepository.InsertManyAsync(missing, cancellationToken: cancellationToken);
    }

    private async Task<List<long>> EnsureAllMenusAsync(CancellationToken cancellationToken)
    {
        var newMenuIds = new List<long>();

        var (systemDirId, systemDirMenuIds) = await EnsureSystemDirectoryAsync(cancellationToken);
        newMenuIds.AddRange(systemDirMenuIds);

        newMenuIds.AddRange(await EnsureUserMenusAsync(systemDirId, cancellationToken));
        newMenuIds.AddRange(await EnsureRoleMenusAsync(systemDirId, cancellationToken));
        newMenuIds.AddRange(await EnsureMenuMenusAsync(systemDirId, cancellationToken));
        newMenuIds.AddRange(await EnsureDictMenusAsync(systemDirId, cancellationToken));
        newMenuIds.AddRange(await EnsureConfigMenusAsync(systemDirId, cancellationToken));
        newMenuIds.AddRange(await EnsureJobMenusAsync(systemDirId, cancellationToken));
        newMenuIds.AddRange(await EnsureMessageMenusAsync(systemDirId, cancellationToken));
        newMenuIds.AddRange(await EnsureOnlineUserMenusAsync(systemDirId, cancellationToken));
        newMenuIds.AddRange(await EnsurePaymentMenusAsync(cancellationToken));
        newMenuIds.AddRange(await EnsureTicketMenusAsync(cancellationToken));

        return newMenuIds;
    }

    private async Task<(long DirectoryId, List<long> NewMenuIds)> EnsureSystemDirectoryAsync(
        CancellationToken cancellationToken)
    {
        var existingId = await _menuRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => x.Path == "/system" && x.MenuType == MenuType.Directory)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingId > 0)
        {
            return (existingId, []);
        }

        _logger.LogInformation("Seeding system directory menu...");

        var systemDir = await InsertMenuAsync(new Menu
        {
            Name = "系统管理",
            MenuType = MenuType.Directory,
            Path = "/system",
            Icon = "icon-settings",
            Sort = 1,
            IsVisible = true
        }, cancellationToken);

        return (systemDir.Id, [systemDir.Id]);
    }

    private async Task<List<long>> EnsureUserMenusAsync(long parentId, CancellationToken cancellationToken)
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

        return [page.Id, ..buttons.Select(x => x.Id)];
    }

    private async Task<List<long>> EnsureRoleMenusAsync(long parentId, CancellationToken cancellationToken)
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
            Icon = "icon-safe",
            Sort = 2
        }, cancellationToken);

        var buttons = await InsertManyAsync([
            Btn(page.Id, "角色新增", RbacPermissionCodes.System.Role.Create, 1),
            Btn(page.Id, "角色修改", RbacPermissionCodes.System.Role.Update, 2),
            Btn(page.Id, "角色删除", RbacPermissionCodes.System.Role.Delete, 3),
            Btn(page.Id, "分配菜单", RbacPermissionCodes.System.Role.AssignMenus, 4),
        ], cancellationToken);

        return [page.Id, ..buttons.Select(x => x.Id)];
    }

    private async Task<List<long>> EnsureMenuMenusAsync(long parentId, CancellationToken cancellationToken)
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

        return [page.Id, ..buttons.Select(x => x.Id)];
    }

    private async Task<List<long>> EnsureDictMenusAsync(long parentId, CancellationToken cancellationToken)
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

        return [page.Id, ..buttons.Select(x => x.Id)];
    }

    private async Task<List<long>> EnsureConfigMenusAsync(long parentId, CancellationToken cancellationToken)
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

        return [page.Id, ..buttons.Select(x => x.Id)];
    }

    private async Task<List<long>> EnsureJobMenusAsync(long parentId, CancellationToken cancellationToken)
    {
        if (await _menuRepository.AnyAsync(
                x => x.Perms == RbacPermissionCodes.System.Job.List,
                cancellationToken))
        {
            return [];
        }

        _logger.LogInformation("Seeding scheduled job menus...");

        var page = await InsertMenuAsync(new Menu
        {
            ParentId = parentId,
            Name = "定时任务",
            MenuType = MenuType.Menu,
            Perms = RbacPermissionCodes.System.Job.List,
            Path = "/system/job",
            Component = "system/job/index",
            Icon = "clock-circle",
            Sort = 6
        }, cancellationToken);

        var buttons = await InsertManyAsync([
            Btn(page.Id, "任务新增", RbacPermissionCodes.System.Job.Create, 1),
            Btn(page.Id, "任务修改", RbacPermissionCodes.System.Job.Update, 2),
            Btn(page.Id, "任务删除", RbacPermissionCodes.System.Job.Delete, 3),
            Btn(page.Id, "立即执行", RbacPermissionCodes.System.Job.Trigger, 4),
        ], cancellationToken);

        return [page.Id, ..buttons.Select(x => x.Id)];
    }

    private async Task<List<long>> EnsureMessageMenusAsync(long parentId, CancellationToken cancellationToken)
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

        return [page.Id];
    }

    private async Task<List<long>> EnsureOnlineUserMenusAsync(
        long parentId,
        CancellationToken cancellationToken)
    {
        var newMenuIds = new List<long>();

        var page = await _menuRepository.GetQueryable()
            .FirstOrDefaultAsync(
                x => x.Perms == RbacPermissionCodes.System.OnlineUser.List,
                cancellationToken);

        if (page == null)
        {
            _logger.LogInformation("Seeding online user menus...");

            page = await InsertMenuAsync(new Menu
            {
                ParentId = parentId,
                Name = "在线用户",
                MenuType = MenuType.Menu,
                Perms = RbacPermissionCodes.System.OnlineUser.List,
                Path = "/system/online-user",
                Component = "system/online-user/index",
                Icon = "wifi",
                Sort = 7
            }, cancellationToken);

            newMenuIds.Add(page.Id);
        }

        if (!await _menuRepository.AnyAsync(
                x => x.Perms == RbacPermissionCodes.System.OnlineUser.Kick,
                cancellationToken))
        {
            _logger.LogInformation("Seeding online user kick button...");

            var kickButton = await InsertMenuAsync(
                Btn(page.Id, "强制下线", RbacPermissionCodes.System.OnlineUser.Kick, 1),
                cancellationToken);

            newMenuIds.Add(kickButton.Id);
        }

        return newMenuIds;
    }

    private async Task<List<long>> EnsurePaymentMenusAsync(CancellationToken cancellationToken)
    {
        var newMenuIds = new List<long>();

        var (paymentDirId, dirMenuIds) = await EnsurePaymentDirectoryAsync(cancellationToken);
        newMenuIds.AddRange(dirMenuIds);

        newMenuIds.AddRange(await EnsurePaymentChannelMenusAsync(paymentDirId, cancellationToken));
        newMenuIds.AddRange(await EnsurePaymentOrderMenusAsync(paymentDirId, cancellationToken));
        newMenuIds.AddRange(await EnsurePaymentRefundMenusAsync(paymentDirId, cancellationToken));

        return newMenuIds;
    }

    private async Task<(long DirectoryId, List<long> NewMenuIds)> EnsurePaymentDirectoryAsync(
        CancellationToken cancellationToken)
    {
        var existingId = await _menuRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => x.Path == "/payment" && x.MenuType == MenuType.Directory)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingId > 0)
        {
            return (existingId, []);
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

        return (paymentDir.Id, [paymentDir.Id]);
    }

    private async Task<List<long>> EnsurePaymentChannelMenusAsync(
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

        return [channelPage.Id, ..buttons.Select(x => x.Id)];
    }

    private async Task<List<long>> EnsurePaymentOrderMenusAsync(
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

        return [orderPage.Id, ..buttons.Select(x => x.Id)];
    }

    private async Task<List<long>> EnsurePaymentRefundMenusAsync(
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

        return [refundPage.Id];
    }

    private async Task<List<long>> EnsureTicketMenusAsync(CancellationToken cancellationToken)
    {
        var newMenuIds = new List<long>();

        var (ticketDirId, dirMenuIds) = await EnsureTicketDirectoryAsync(cancellationToken);
        newMenuIds.AddRange(dirMenuIds);
        newMenuIds.AddRange(await EnsureWorkTicketMenusAsync(ticketDirId, cancellationToken));
        newMenuIds.AddRange(await EnsureWorkTicketProcessMenusAsync(ticketDirId, cancellationToken));

        return newMenuIds;
    }

    private async Task<(long DirectoryId, List<long> NewMenuIds)> EnsureTicketDirectoryAsync(
        CancellationToken cancellationToken)
    {
        var existingId = await _menuRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => x.Path == "/ticket" && x.MenuType == MenuType.Directory)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingId > 0)
        {
            return (existingId, []);
        }

        _logger.LogInformation("Seeding ticket directory menu...");

        var ticketDir = await InsertMenuAsync(new Menu
        {
            Name = "工单管理",
            MenuType = MenuType.Directory,
            Path = "/ticket",
            Icon = "customer-service",
            Sort = 15,
            IsVisible = true
        }, cancellationToken);

        return (ticketDir.Id, [ticketDir.Id]);
    }

    private async Task<List<long>> EnsureWorkTicketMenusAsync(
        long parentId,
        CancellationToken cancellationToken)
    {
        if (await _menuRepository.AnyAsync(
                x => x.Perms == RbacPermissionCodes.Ticket.Work.List,
                cancellationToken))
        {
            return [];
        }

        _logger.LogInformation("Seeding work ticket menus...");

        var workPage = await InsertMenuAsync(new Menu
        {
            ParentId = parentId,
            Name = "工单列表",
            MenuType = MenuType.Menu,
            Perms = RbacPermissionCodes.Ticket.Work.List,
            Path = "/ticket/work",
            Component = "ticket/work/index",
            Icon = "file",
            Sort = 1
        }, cancellationToken);

        var buttons = await InsertManyAsync([
            Btn(workPage.Id, "工单新增", RbacPermissionCodes.Ticket.Work.Create, 1),
            Btn(workPage.Id, "工单修改", RbacPermissionCodes.Ticket.Work.Update, 2),
            Btn(workPage.Id, "工单删除", RbacPermissionCodes.Ticket.Work.Delete, 3),
        ], cancellationToken);

        return [workPage.Id, ..buttons.Select(x => x.Id)];
    }

    private async Task<List<long>> EnsureWorkTicketProcessMenusAsync(
        long parentId,
        CancellationToken cancellationToken)
    {
        if (await _menuRepository.AnyAsync(
                x => x.Perms == RbacPermissionCodes.Ticket.Work.Process,
                cancellationToken))
        {
            return [];
        }

        _logger.LogInformation("Seeding work ticket process menus...");

        var processPage = await InsertMenuAsync(new Menu
        {
            ParentId = parentId,
            Name = "工单处理",
            MenuType = MenuType.Menu,
            Perms = RbacPermissionCodes.Ticket.Work.Process,
            Path = "/ticket/process",
            Component = "ticket/process/index",
            Icon = "tool",
            Sort = 2
        }, cancellationToken);

        return [processPage.Id];
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
