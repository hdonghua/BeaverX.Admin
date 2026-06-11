using BeaverX.Admin.Infrastructure.Scheduling;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeaverX.Admin.Infrastructure;

public static class HangfireServiceCollectionExtensions
{
    public static IServiceCollection AddBeaverXHangfire(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is required.");

        services.Configure<HangfireOptions>(configuration.GetSection(HangfireOptions.SectionName));
        HangfireDashboardExtensions.RegisterCustomDashboardExtensions();
        var hangfireOptions = configuration.GetSection(HangfireOptions.SectionName).Get<HangfireOptions>()
            ?? new HangfireOptions();

        services.AddHttpClient(nameof(HttpApiScheduledJobRunner));
        services.AddScoped<HttpApiScheduledJobRunner>();
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                options => options.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions
                {
                    SchemaName = hangfireOptions.SchemaName
                }));

        services.AddHangfireServer(options =>
        {
            options.ServerName = $"{Environment.MachineName}:{Environment.ProcessId}";
        });

        services.AddHostedService<ScheduledJobSyncHostedService>();

        return services;
    }

    public static IApplicationBuilder UseBeaverXHangfire(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        var hangfireOptions = configuration.GetSection(HangfireOptions.SectionName).Get<HangfireOptions>()
            ?? new HangfireOptions();

        if (hangfireOptions.EnableDashboard)
        {
            app.UseHangfireDashboard(hangfireOptions.DashboardPath, new DashboardOptions
            {
                //Authorization = [new HangfireDashboardAuthorizationFilter()],
                DashboardTitle = "BeaverX Jobs",
                IgnoreAntiforgeryToken = true
            });
        }

        return app;
    }
}
