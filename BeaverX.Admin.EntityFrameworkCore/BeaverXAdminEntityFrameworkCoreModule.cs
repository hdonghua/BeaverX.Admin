using BeaverX.Admin.Domain;
using BeaverX.Core.Modules;
using BeaverX.EntityFrameworkCore;
using BeaverX.EntityFrameworkCore.DependencyInjection;
using BeaverX.EntityFrameworkCore.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BeaverX.Admin.EntityFrameworkCore
{
    [DependsOn(
        typeof(BeaverXAdminDomainModule),
        typeof(BeaverXEntityFrameworkCoreModule),
        typeof(BeaverXEntityFrameworkCorePostgreSqlModule)
    )]
    public class BeaverXAdminEntityFrameworkCoreModule : BeaverXModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            var configuration = context.Configuration;

            services.Replace(ServiceDescriptor.Singleton<IDbDriverOptionsBuilder, AdminPostgreSqlDbDriverOptionsBuilder>());
            services.AddBeaverXDbContext<AdminDbContext>(configuration.GetConnectionString("Default")!);
        }
    }
}
