using BeaverX.Admin.Domain.DataSeeder;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Messages;

public class MessageMenuSeeder : IScopedDependency, IDataSeeder
{
    private readonly IRepository<Menu> _menuRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<RoleMenu> _roleMenuRepository;
    private readonly ILogger<MessageMenuSeeder> _logger;

    public MessageMenuSeeder(
        IRepository<Menu> menuRepository,
        IRepository<Role> roleRepository,
        IRepository<RoleMenu> roleMenuRepository,
        ILogger<MessageMenuSeeder> logger)
    {
        _menuRepository = menuRepository;
        _roleRepository = roleRepository;
        _roleMenuRepository = roleMenuRepository;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _menuRepository.AnyAsync(
                x => x.Perms == RbacPermissionCodes.System.Message.Send,
                cancellationToken))
        {
            return;
        }

        var systemDir = await _menuRepository.GetQueryable()
            .FirstOrDefaultAsync(x => x.Path == "/system" && x.MenuType == MenuType.Directory, cancellationToken);

        if (systemDir == null)
        {
            _logger.LogWarning("Message menu seed skipped: system directory menu not found.");
            return;
        }

        _logger.LogInformation("Seeding site message menus...");

        var page = new Menu
        {
            ParentId = systemDir.Id,
            Name = "发送站内信",
            MenuType = MenuType.Menu,
            Perms = RbacPermissionCodes.System.Message.Send,
            Path = "/system/message",
            Component = "system/message/send",
            Icon = "message",
            Sort = 6,
            IsVisible = true
        };
        await _menuRepository.InsertAsync(page, cancellationToken: cancellationToken);

        var adminRole = await _roleRepository.GetQueryable()
            .FirstOrDefaultAsync(x => x.Code == RbacPermissionCodes.SuperAdmin, cancellationToken);

        if (adminRole != null)
        {
            await _roleMenuRepository.InsertAsync(
                new RoleMenu { RoleId = adminRole.Id, MenuId = page.Id },
                cancellationToken: cancellationToken);
        }
    }
}
