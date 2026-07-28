using BeaverX.Admin.Domain;
using BeaverX.Admin.EntityFrameworkCore.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.PostgreSql;
using Volo.Abp.Modularity;

namespace BeaverX.Admin.EntityFrameworkCore;

[DependsOn(
    typeof(BeaverXAdminDomainModule),
    typeof(AbpEntityFrameworkCorePostgreSqlModule)
)]
public class BeaverXAdminEntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<AdminDbContext>(options =>
        {
            options.AddDefaultRepositories(includeAllEntities: true);
        });

        Configure<AbpDbContextOptions>(options =>
        {
            options.Configure(dbContextOptions =>
            {
                dbContextOptions.UseNpgsql();

                dbContextOptions.DbContextOptions.AddInterceptors(new UtcDateTimeSaveChangesInterceptor());
#if DEBUG
                dbContextOptions.DbContextOptions.EnableSensitiveDataLogging();
#endif
            });
        });
    }
}
