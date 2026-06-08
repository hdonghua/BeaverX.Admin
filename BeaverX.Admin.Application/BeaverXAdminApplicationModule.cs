using BeaverX.Admin.Application.Contracts;
using BeaverX.Admin.Application.Exports;
using BeaverX.Admin.Application.Rbac;
using BeaverX.Admin.Application.Storage;
using BeaverX.Admin.Domain;
using BeaverX.Core.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;

namespace BeaverX.Admin.Application
{
    [DependsOn(
        typeof(BeaverXAdminApplicationContractModule),
        typeof(BeaverXAdminDomainModule)
    )]
    public class BeaverXAdminApplicationModule : BeaverXModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            var configuration = context.Configuration;

            services.AddHostedService<ExportTaskRecoveryHostedService>();

            services.Configure<JwtOptions>(
                configuration.GetSection(JwtOptions.SectionName));

            services.Configure<MinioOptions>(
                configuration.GetSection(MinioOptions.SectionName));

            var minioOptions = configuration
                .GetSection(MinioOptions.SectionName)
                .Get<MinioOptions>();

            if (minioOptions != null &&
                !string.IsNullOrWhiteSpace(minioOptions.Endpoint) &&
                !string.IsNullOrWhiteSpace(minioOptions.AccessKey) &&
                !string.IsNullOrWhiteSpace(minioOptions.SecretKey))
            {
                var (endpoint, useSsl) = MinioEndpointHelper.Parse(minioOptions.Endpoint, minioOptions.UseSsl);
                services.AddMinio(client => client
                    .WithEndpoint(endpoint)
                    .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey)
                    .WithSSL(useSsl)
                    .Build());
            }
        }
    }
}
