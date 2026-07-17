using BeaverX.Admin.Domain;
using BeaverX.Admin.SqlSugar.Interceptors;
using BeaverX.Core.Modules;
using BeaverX.Data.SqlSugar;
using BeaverX.Data.SqlSugar.DependencyInjection;
using BeaverX.Domain.IdGeneration;
using IdGen.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeaverX.Admin.EntityFrameworkCore
{
    [DependsOn(
        typeof(BeaverXAdminDomainModule),
        typeof(BeaverXDataSqlSugarModule)
    )]
    public class BeaverXAdminSqlSugarModule : BeaverXModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            var configuration = context.Configuration;

            var idGenOptions = configuration.GetSection(IdGenOptions.SectionName).Get<IdGenOptions>()
                ?? new IdGenOptions();
            services.Configure<IdGenOptions>(configuration.GetSection(IdGenOptions.SectionName));
            services.AddIdGen(idGenOptions.GeneratorId);
            services.AddSingleton<IIdGenerator<long>, SnowflakeEntityIdGenerator>();

            services.AddBeaverXSqlSugar(options =>
            {
                options.ConnectionString = configuration.GetConnectionString("Default")!;
                options.DbType = global::SqlSugar.DbType.MySql;
                options.AddEntitiesFromAssembly(typeof(BeaverXAdminDomainModule).Assembly);
                options.NormalizeEntityBeforeWrite = UtcDateTimeSaveChangesInterceptor.NormalizeDateTimes;
            });
        }
    }
}
