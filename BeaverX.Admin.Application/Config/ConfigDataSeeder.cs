using BeaverX.Admin.Domain.Config;
using BeaverX.Admin.Domain.DataSeeder;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Config;

public class ConfigDataSeeder : IScopedDependency, IDataSeeder
{
  private readonly IRepository<SysConfig> _configRepository;
  private readonly ILogger<ConfigDataSeeder> _logger;

  public ConfigDataSeeder(
    IRepository<SysConfig> configRepository,
    ILogger<ConfigDataSeeder> logger)
  {
    _configRepository = configRepository;
    _logger = logger;
  }

  public async Task SeedAsync(CancellationToken cancellationToken = default)
  {
    await EnsureConfigAsync(
      "sys.site.name",
      () => new SysConfig
      {
        Key = "sys.site.name",
        Label = "站点名称",
        Value = "BeaverX Admin",
        Group = "系统",
        Sort = 1,
        IsEnabled = true,
        Remark = "后台管理系统显示名称",
      },
      cancellationToken);

    await EnsureConfigAsync(
      "sys.site.copyright",
      () => new SysConfig
      {
        Key = "sys.site.copyright",
        Label = "版权信息",
        Value = "Copyright © BeaverX",
        Group = "系统",
        Sort = 2,
        IsEnabled = true,
      },
      cancellationToken);

    await EnsureConfigAsync(
      "sys.user.initPassword",
      () => new SysConfig
      {
        Key = "sys.user.initPassword",
        Label = "用户初始密码",
        Value = "123456",
        Group = "安全",
        Sort = 1,
        IsEnabled = true,
        Remark = "新建用户时的默认密码",
      },
      cancellationToken);

    await EnsureConfigAsync(
      "sys.login.captcha",
      () => new SysConfig
      {
        Key = "sys.login.captcha",
        Label = "登录验证码",
        Value = "false",
        Group = "安全",
        Sort = 2,
        IsEnabled = true,
        Remark = "是否启用登录验证码",
      },
      cancellationToken);
  }

  private async Task EnsureConfigAsync(
    string key,
    Func<SysConfig> factory,
    CancellationToken cancellationToken)
  {
    if (await _configRepository.AnyAsync(x => x.Key == key, cancellationToken))
    {
      return;
    }

    _logger.LogInformation("Seeding config {Key}...", key);
    await _configRepository.InsertAsync(factory(), cancellationToken: cancellationToken);
  }
}
