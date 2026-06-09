using BeaverX.Admin.Domain.DataSeeder;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Payment;

/// <summary>
/// 增量种子：为已有环境补充「退款记录」菜单页
/// </summary>
public class PaymentRefundMenuSeeder : IScopedDependency, IDataSeeder
{
  private readonly IRepository<Menu> _menuRepository;
  private readonly IRepository<Role> _roleRepository;
  private readonly IRepository<RoleMenu> _roleMenuRepository;
  private readonly ILogger<PaymentRefundMenuSeeder> _logger;

  public PaymentRefundMenuSeeder(
    IRepository<Menu> menuRepository,
    IRepository<Role> roleRepository,
    IRepository<RoleMenu> roleMenuRepository,
    ILogger<PaymentRefundMenuSeeder> logger)
  {
    _menuRepository = menuRepository;
    _roleRepository = roleRepository;
    _roleMenuRepository = roleMenuRepository;
    _logger = logger;
  }

  public async Task SeedAsync(CancellationToken cancellationToken = default)
  {
    if (await _menuRepository.AnyAsync(
        x => x.Path == "/payment/refund" && x.MenuType == MenuType.Menu,
        cancellationToken))
    {
      return;
    }

    var paymentDir = await _menuRepository.GetQueryable()
      .FirstOrDefaultAsync(x => x.Path == "/payment" && x.MenuType == MenuType.Directory, cancellationToken);

    if (paymentDir == null)
    {
      _logger.LogWarning("Payment refund menu seed skipped: payment directory not found.");
      return;
    }

    _logger.LogInformation("Seeding payment refund menu...");

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
    await _menuRepository.InsertAsync(refundPage, cancellationToken: cancellationToken);

    var adminRole = await _roleRepository.GetQueryable()
      .FirstOrDefaultAsync(x => x.Code == RbacPermissionCodes.SuperAdmin, cancellationToken);

    if (adminRole != null)
    {
      await _roleMenuRepository.InsertAsync(
        new RoleMenu { RoleId = adminRole.Id, MenuId = refundPage.Id },
        cancellationToken: cancellationToken);
    }
  }
}
