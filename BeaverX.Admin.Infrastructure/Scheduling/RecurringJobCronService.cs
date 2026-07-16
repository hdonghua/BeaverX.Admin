using BeaverX.Admin.Application.Scheduling;
using BeaverX.Admin.Domain.Scheduling;
using BeaverX.Core.Dependency;
using BeaverX.Data.SqlSugar.Repositories;
using BeaverX.Domain.Repositories;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace BeaverX.Admin.Infrastructure.Scheduling;

public interface IRecurringJobCronService
{
    Task UpdateCronAsync(
        string recurringJobId,
        string cronExpression,
        CancellationToken cancellationToken = default);

    Task UpdateTimeZoneAsync(
        string recurringJobId,
        string timeZoneId,
        CancellationToken cancellationToken = default);
}

public class RecurringJobCronService : IRecurringJobCronService, ISingletonDependency
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRecurringJobPauseService _pauseService;

    public RecurringJobCronService(
        IServiceScopeFactory scopeFactory,
        IRecurringJobPauseService pauseService)
    {
        _scopeFactory = scopeFactory;
        _pauseService = pauseService;
    }

    public async Task UpdateCronAsync(
        string recurringJobId,
        string cronExpression,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recurringJobId))
        {
            throw new InvalidOperationException("任务编号不能为空");
        }

        CronExpressionHelper.EnsureValid(cronExpression);
        var normalizedCron = cronExpression.Trim();

        if (await _pauseService.IsPausedAsync(recurringJobId, cancellationToken))
        {
            await _pauseService.UpdatePausedCronAsync(recurringJobId, normalizedCron, cancellationToken);
            return;
        }

        using var connection = JobStorage.Current.GetConnection();
        var existing = connection.GetRecurringJobs()
            .FirstOrDefault(x => string.Equals(x.Id, recurringJobId, StringComparison.Ordinal));

        if (existing == null)
        {
            throw new InvalidOperationException($"周期性任务不存在: {recurringJobId}");
        }

        if (existing.Job == null)
        {
            throw new InvalidOperationException("该任务定义无效，无法更新 Cron");
        }

        var timeZone = ResolveTimeZone(existing.TimeZoneId);
        var manager = new RecurringJobManager();
        manager.AddOrUpdate(
            recurringJobId,
            existing.Job,
            normalizedCron,
            new RecurringJobOptions
            {
                TimeZone = timeZone
            });

        await SyncBusinessJobAsync(recurringJobId, normalizedCron, existing.TimeZoneId, cancellationToken);
    }

    public async Task UpdateTimeZoneAsync(
        string recurringJobId,
        string timeZoneId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recurringJobId))
        {
            throw new InvalidOperationException("任务编号不能为空");
        }

        var normalizedTimeZoneId = timeZoneId.Trim();
        EnsureValidTimeZone(normalizedTimeZoneId);

        if (await _pauseService.IsPausedAsync(recurringJobId, cancellationToken))
        {
            await _pauseService.UpdatePausedTimeZoneAsync(
                recurringJobId,
                normalizedTimeZoneId,
                cancellationToken);
            return;
        }

        using var connection = JobStorage.Current.GetConnection();
        var existing = connection.GetRecurringJobs()
            .FirstOrDefault(x => string.Equals(x.Id, recurringJobId, StringComparison.Ordinal));

        if (existing == null)
        {
            throw new InvalidOperationException($"周期性任务不存在: {recurringJobId}");
        }

        if (existing.Job == null)
        {
            throw new InvalidOperationException("该任务定义无效，无法更新时区");
        }

        if (string.IsNullOrWhiteSpace(existing.Cron))
        {
            throw new InvalidOperationException("该任务 Cron 无效，无法更新时区");
        }

        var timeZone = ResolveTimeZone(normalizedTimeZoneId);
        var manager = new RecurringJobManager();
        manager.AddOrUpdate(
            recurringJobId,
            existing.Job,
            existing.Cron,
            new RecurringJobOptions
            {
                TimeZone = timeZone
            });

        await SyncBusinessJobTimeZoneAsync(recurringJobId, normalizedTimeZoneId, cancellationToken);
    }

    private static void EnsureValidTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new InvalidOperationException("时区不能为空");
        }

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException ex)
        {
            throw new InvalidOperationException($"无效的时区: {timeZoneId}", ex);
        }
        catch (InvalidTimeZoneException ex)
        {
            throw new InvalidOperationException($"无效的时区: {timeZoneId}", ex);
        }
    }

    private async Task SyncBusinessJobTimeZoneAsync(
        string recurringJobId,
        string timeZoneId,
        CancellationToken cancellationToken)
    {
        const string prefix = "scheduled-job:";
        if (!recurringJobId.StartsWith(prefix, StringComparison.Ordinal))
        {
            return;
        }

        if (!long.TryParse(recurringJobId[prefix.Length..], out var jobId))
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISugarRepository<ScheduledJob>>();
        var job = await repository.FindAsync(x => x.Id == jobId, cancellationToken);
        if (job == null)
        {
            return;
        }

        job.TimeZoneId = timeZoneId;
        await repository.UpdateAsync(job, cancellationToken: cancellationToken);
    }

    private async Task SyncBusinessJobAsync(
        string recurringJobId,
        string cronExpression,
        string? timeZoneId,
        CancellationToken cancellationToken)
    {
        const string prefix = "scheduled-job:";
        if (!recurringJobId.StartsWith(prefix, StringComparison.Ordinal))
        {
            return;
        }

        if (!long.TryParse(recurringJobId["scheduled-job:".Length..], out var jobId))
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISugarRepository<ScheduledJob>>();
        var job = await repository.FindAsync(x => x.Id == jobId, cancellationToken);
        if (job == null)
        {
            return;
        }

        job.CronExpression = cronExpression;
        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            job.TimeZoneId = timeZoneId;
        }

        await repository.UpdateAsync(job, cancellationToken: cancellationToken);
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
