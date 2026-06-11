using BeaverX.Admin.Application.Scheduling;
using BeaverX.Admin.Domain.Scheduling;
using BeaverX.Core.Dependency;
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
}

public class RecurringJobCronService : IRecurringJobCronService, ISingletonDependency
{
    private readonly IServiceScopeFactory _scopeFactory;

    public RecurringJobCronService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
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
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<ScheduledJob>>();
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
