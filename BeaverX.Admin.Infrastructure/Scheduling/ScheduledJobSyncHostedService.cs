using BeaverX.Admin.Application.Contracts.Scheduling;
using BeaverX.Admin.Domain.Scheduling;
using BeaverX.Domain.Repositories;
using Hangfire;
using Hangfire.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BeaverX.Admin.Infrastructure.Scheduling;

public class ScheduledJobSyncHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<HangfireOptions> _hangfireOptions;
    private readonly ILogger<ScheduledJobSyncHostedService> _logger;

    public ScheduledJobSyncHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<HangfireOptions> hangfireOptions,
        ILogger<ScheduledJobSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _hangfireOptions = hangfireOptions;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_hangfireOptions.Value.SyncBusinessJobsOnStartup)
        {
            _logger.LogInformation("Skip syncing business scheduled jobs on startup (SyncBusinessJobsOnStartup=false)");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IRepository<ScheduledJob>>();
        var registrar = scope.ServiceProvider.GetRequiredService<IHangfireScheduledJobRegistrar>();
        var pauseService = scope.ServiceProvider.GetRequiredService<IRecurringJobPauseService>();

        var jobs = await jobRepository.GetQueryable()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        IReadOnlyList<RecurringJobDto> hangfireJobs = [];
        if (_hangfireOptions.Value.BusinessJobStartupSyncMode == BusinessJobStartupSyncMode.MergeFromHangfire)
        {
            using var connection = JobStorage.Current.GetConnection();
            hangfireJobs = connection.GetRecurringJobs();
        }

        foreach (var job in jobs)
        {
            var hangfireJobId = HangfireScheduledJobRegistrar.BuildRecurringJobId(job.Id);

            if (_hangfireOptions.Value.BusinessJobStartupSyncMode == BusinessJobStartupSyncMode.MergeFromHangfire)
            {
                var hangfireJob = hangfireJobs.FirstOrDefault(x =>
                    string.Equals(x.Id, hangfireJobId, StringComparison.Ordinal));

                var isPaused = await pauseService.IsPausedAsync(hangfireJobId, cancellationToken);
                if (!isPaused &&
                    hangfireJob?.Cron != null &&
                    !string.Equals(hangfireJob.Cron, job.CronExpression, StringComparison.Ordinal))
                {
                    job.CronExpression = hangfireJob.Cron;
                    if (!string.IsNullOrWhiteSpace(hangfireJob.TimeZoneId))
                    {
                        job.TimeZoneId = hangfireJob.TimeZoneId;
                    }

                    await jobRepository.UpdateAsync(job, cancellationToken: cancellationToken);
                    _logger.LogInformation(
                        "Merged Cron from Hangfire for job {JobId}: {Cron}",
                        job.Id,
                        job.CronExpression);
                }
            }

            var cronExpression = job.CronExpression;
            if (await pauseService.IsPausedAsync(hangfireJobId, cancellationToken))
            {
                cronExpression = Cron.Never();
            }

            registrar.Register(new ScheduledJobRegistration
            {
                JobId = job.Id,
                CronExpression = cronExpression,
                TimeZoneId = job.TimeZoneId,
                IsEnabled = job.IsEnabled
            });
        }

        _logger.LogInformation(
            "Synchronized {Count} business scheduled jobs to Hangfire (mode={Mode})",
            jobs.Count,
            _hangfireOptions.Value.BusinessJobStartupSyncMode);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
