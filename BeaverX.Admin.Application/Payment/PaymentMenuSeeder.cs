using BeaverX.Admin.Domain.DataSeeder;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Payment;

public class PaymentMenuSeeder : IScopedDependency, IDataSeeder
{
  private readonly IRepository<Menu> _menuRepository;
  private readonly IRepository<Role> _roleRepository;
  private readonly IRepository<RoleMenu> _roleMenuRepository;
  private readonly ILogger<PaymentMenuSeeder> _logger;

  public PaymentMenuSeeder(
    IRepository<Menu> menuRepository,
    IRepository<Role> roleRepository,
    IRepository<RoleMenu> roleMenuRepository,
    ILogger<PaymentMenuSeeder> logger)
  {
    _menuRepository = menuRepository;
    _roleRepository = roleRepository;
    _roleMenuRepository = roleMenuRepository;
    _logger = logger;
  }

  public async Task SeedAsync(CancellationToken cancellationToken = default)
  {
    if (await _menuRepository.AnyAsync(
        x => x.Perms == RbacPermissionCodes.Payment.Channel.List,
        cancellationToken))
    {
      return;
    }

    _logger.LogInformation("Seeding payment menus...");

    var paymentDir = new Menu
    {
      Name = "支付管理",
      MenuType = MenuType.Directory,
      Path = "/payment",
      Icon = "alipay-circle",
      Sort = 20,
      IsVisible = true,
    };
    await _menuRepository.InsertAsync(paymentDir, cancellationToken: cancellationToken);

    var channelPage = new Menu
    {
      ParentId = paymentDir.Id,
      Name = "支付渠道",
      MenuType = MenuType.Menu,
      Perms = RbacPermissionCodes.Payment.Channel.List,
      Path = "/payment/channel",
      Component = "payment/channel/index",
      Icon = "icon-settings",
      Sort = 1,
      IsVisible = true,
    };
    var orderPage = new Menu
    {
      ParentId = paymentDir.Id,
      Name = "支付订单",
      MenuType = MenuType.Menu,
      Perms = RbacPermissionCodes.Payment.Order.List,
      Path = "/payment/order",
      Component = "payment/order/index",
      Icon = "icon-list",
      Sort = 2,
      IsVisible = true,
    };
    var refundPage = new Menu
    {
      ParentId = paymentDir.Id,
      Name = "退款记录",
      MenuType = MenuType.Menu,
      Perms = RbacPermissionCodes.Payment.Refund.List,
      Path = "/payment/refund",
      Component = "payment/refund/index",
      Icon = "icon-swap",
      Sort = 3,
      IsVisible = true,
    };

    await _menuRepository.InsertAsync(channelPage, cancellationToken: cancellationToken);
    await _menuRepository.InsertAsync(orderPage, cancellationToken: cancellationToken);
    await _menuRepository.InsertAsync(refundPage, cancellationToken: cancellationToken);

    var buttons = new[]
    {
      Btn(channelPage.Id, "渠道新增", RbacPermissionCodes.Payment.Channel.Create, 1),
      Btn(channelPage.Id, "渠道修改", RbacPermissionCodes.Payment.Channel.Update, 2),
      Btn(channelPage.Id, "渠道删除", RbacPermissionCodes.Payment.Channel.Delete, 3),
      Btn(orderPage.Id, "创建订单", RbacPermissionCodes.Payment.Order.Create, 1),
      Btn(orderPage.Id, "查询订单", RbacPermissionCodes.Payment.Order.Query, 2),
      Btn(orderPage.Id, "关闭订单", RbacPermissionCodes.Payment.Order.Close, 3),
      Btn(orderPage.Id, "订单退款", RbacPermissionCodes.Payment.Order.Refund, 4),
      Btn(orderPage.Id, "沙箱支付", RbacPermissionCodes.Payment.Order.SandboxPay, 5),
    };
    await _menuRepository.InsertManyAsync(buttons, cancellationToken: cancellationToken);

    var adminRole = await _roleRepository.GetQueryable()
      .FirstOrDefaultAsync(x => x.Code == RbacPermissionCodes.SuperAdmin, cancellationToken);

    if (adminRole != null)
    {
      var menuIds = new[] { paymentDir.Id, channelPage.Id, orderPage.Id, refundPage.Id }
        .Concat(buttons.Select(x => x.Id));
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
    IsVisible = false,
  };
}
