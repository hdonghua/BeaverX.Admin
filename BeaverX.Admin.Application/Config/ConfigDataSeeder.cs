using BeaverX.Admin.Domain.Config;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Config;

public class ConfigDataSeeder : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<SysConfig, Guid> _configRepository;
    private readonly ILogger<ConfigDataSeeder> _logger;

    public ConfigDataSeeder(
      IRepository<SysConfig, Guid> configRepository,
      ILogger<ConfigDataSeeder> logger)
    {
        _configRepository = configRepository;
        _logger = logger;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        var cancellationToken = CancellationToken.None;
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
              Value = "Admin@123",
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
