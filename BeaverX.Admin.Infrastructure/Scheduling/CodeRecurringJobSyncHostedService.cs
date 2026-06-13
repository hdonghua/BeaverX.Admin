using BeaverX.Admin.Application.Contracts.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Infrastructure.Scheduling;

/// <summary>
/// 启动时将容器中所有 <see cref="IRecurringJob"/> 实现按 Cron 注册到 Hangfire（Job Id 为类型全名）。
/// </summary>
public class CodeRecurringJobSyncHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CodeRecurringJobSyncHostedService> _logger;

    public CodeRecurringJobSyncHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<CodeRecurringJobSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobs = scope.ServiceProvider.GetServices<IRecurringJob>().ToList();

        foreach (var job in jobs)
        {
            var jobType = job.GetType();
            var jobId = jobType.FullName
                ?? throw new InvalidOperationException($"Recurring job type {jobType.Name} has no full name.");

            if (string.IsNullOrWhiteSpace(job.CronExpression))
            {
                _logger.LogWarning(
                    "Skip registering recurring job {JobId}: CronExpression is empty",
                    jobId);
                continue;
            }

            HangfireRecurringJobStartup.AddOrUpdateIfNotExists<CodeRecurringJobRunner>(
                jobId,
                runner => runner.ExecuteAsync(jobId, CancellationToken.None),
                job.CronExpression.Trim());

            _logger.LogInformation(
                "Registered recurring job {JobId} with cron {Cron}",
                jobId,
                job.CronExpression);
        }

        _logger.LogInformation("Synchronized {Count} code recurring jobs to Hangfire", jobs.Count);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
