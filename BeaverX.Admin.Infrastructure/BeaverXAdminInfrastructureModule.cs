using BeaverX.Admin.Application;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Storage;
using BeaverX.Admin.EntityFrameworkCore;
using BeaverX.Admin.Infrastructure.Caching;
using BeaverX.Admin.Infrastructure.Storage;
using BeaverX.Core.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using Savorboard.CAP.InMemoryMessageQueue;

namespace BeaverX.Admin.Infrastructure;

[DependsOn(
    typeof(BeaverXAdminEntityFrameworkCoreModule),
    typeof(BeaverXAdminApplicationModule)
)]
public class BeaverXAdminInfrastructureModule : BeaverXModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        var configuration = context.Configuration;

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));

        services.AddBeaverXCache(configuration);
        ConfigureMinio(services, configuration);
        ConfigureCap(services, configuration);

        services.AddHostedService<Exports.ExportTaskRecoveryHostedService>();
    }

    private static void ConfigureMinio(IServiceCollection services, IConfiguration configuration)
    {
        var minioOptions = configuration
            .GetSection(MinioOptions.SectionName)
            .Get<MinioOptions>();

        if (minioOptions == null ||
            string.IsNullOrWhiteSpace(minioOptions.Endpoint) ||
            string.IsNullOrWhiteSpace(minioOptions.AccessKey) ||
            string.IsNullOrWhiteSpace(minioOptions.SecretKey))
        {
            return;
        }

        var (endpoint, useSsl) = MinioEndpointHelper.Parse(minioOptions.Endpoint, minioOptions.UseSsl);
        services.AddMinio(client => client
            .WithEndpoint(endpoint)
            .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey)
            .WithSSL(useSsl)
            .Build());
    }

    private static void ConfigureCap(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is required.");

        services.AddCap(options =>
        {
            options.UsePostgreSql(postgresOptions =>
            {
                postgresOptions.ConnectionString = connectionString;
                postgresOptions.Schema = "cap";
            });
            options.UseInMemoryMessageQueue();
            options.FailedRetryCount = 5;
            options.FailedRetryInterval = 60;
            options.CollectorCleaningInterval = 3600;
            options.SucceedMessageExpiredAfter = 24 * 3600;
        });
    }
}
