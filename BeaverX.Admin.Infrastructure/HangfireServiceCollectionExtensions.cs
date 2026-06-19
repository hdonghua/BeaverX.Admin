using BeaverX.Admin.Infrastructure.Scheduling;
using Hangfire;
using Hangfire.MySql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Transactions;

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
        services.AddScoped<CodeRecurringJobRunner>();
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseStorage(new MySqlStorage(
                EnsureMySqlHangfireConnectionString(connectionString),
                new MySqlStorageOptions
                {
                    TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    CountersAggregateInterval = TimeSpan.FromMinutes(5),
                    PrepareSchemaIfNecessary = true,
                    DashboardJobListLimit = 50000,
                    TablesPrefix = hangfireOptions.SchemaName
                })));

        services.AddHangfireServer(options =>
        {
            options.ServerName = $"{Environment.MachineName}:{Environment.ProcessId}";
        });

        services.AddHostedService<ScheduledJobSyncHostedService>();
        services.AddHostedService<CodeRecurringJobSyncHostedService>();

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
            app.UseMiddleware<HangfireBasicAuthMiddleware>();
            app.UseHangfireDashboard(hangfireOptions.DashboardPath, new DashboardOptions
            {
                Authorization = [new HangfireDashboardPassthroughAuthorizationFilter()],
                DashboardTitle = "BeaverX Jobs",
                IgnoreAntiforgeryToken = true
            });
        }

        return app;
    }

    /// <summary>
    /// Hangfire.MySqlStorage 依赖用户变量，连接串需包含 Allow User Variables=True。
    /// </summary>
    private static string EnsureMySqlHangfireConnectionString(string connectionString)
    {
        if (connectionString.Contains("Allow User Variables", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var trimmed = connectionString.TrimEnd();
        var separator = trimmed.EndsWith(';') ? string.Empty : ";";
        return $"{trimmed}{separator}Allow User Variables=True;";
    }
}
