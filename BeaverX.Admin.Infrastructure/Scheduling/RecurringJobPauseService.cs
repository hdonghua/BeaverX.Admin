using BeaverX.Admin.Domain.Scheduling;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace BeaverX.Admin.Infrastructure.Scheduling;

public interface IRecurringJobPauseService
{
    Task<bool> IsPausedAsync(string recurringJobId, CancellationToken cancellationToken = default);

    Task PauseAsync(string recurringJobId, CancellationToken cancellationToken = default);

    Task ResumeAsync(string recurringJobId, CancellationToken cancellationToken = default);

    Task UpdatePausedCronAsync(
        string recurringJobId,
        string cronExpression,
        CancellationToken cancellationToken = default);

    Task UpdatePausedTimeZoneAsync(
        string recurringJobId,
        string timeZoneId,
        CancellationToken cancellationToken = default);
}

public class RecurringJobPauseService : IRecurringJobPauseService, ISingletonDependency
{
    private const string PausedHashKey = "beaverx:recurring:paused";
    private const string PausedStateSeparator = "::";

    private readonly IServiceScopeFactory _scopeFactory;

    public RecurringJobPauseService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Task<bool> IsPausedAsync(string recurringJobId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureJobId(recurringJobId);

        using var connection = JobStorage.Current.GetConnection();
        var entries = GetHashEntries(connection);
        return Task.FromResult(entries.ContainsKey(recurringJobId));
    }

    public async Task PauseAsync(string recurringJobId, CancellationToken cancellationToken = default)
    {
        EnsureJobId(recurringJobId);

        using var connection = JobStorage.Current.GetConnection();
        var existing = GetRecurringJobOrThrow(connection, recurringJobId);

        if (await IsPausedAsync(recurringJobId, cancellationToken))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(existing.Cron) ||
            string.Equals(existing.Cron, Cron.Never(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("该任务已停止调度，无法暂停");
        }

        if (existing.Job == null)
        {
            throw new InvalidOperationException("该任务定义无效，无法暂停");
        }

        SavePausedState(
            connection,
            recurringJobId,
            existing.Cron,
            existing.TimeZoneId);

        var manager = new RecurringJobManager();
        manager.AddOrUpdate(
            recurringJobId,
            existing.Job,
            Cron.Never(),
            new RecurringJobOptions
            {
                TimeZone = ResolveTimeZone(existing.TimeZoneId)
            });
    }

    public async Task ResumeAsync(string recurringJobId, CancellationToken cancellationToken = default)
    {
        EnsureJobId(recurringJobId);

        using var connection = JobStorage.Current.GetConnection();
        var existing = GetRecurringJobOrThrow(connection, recurringJobId);

        if (existing.Job == null)
        {
            throw new InvalidOperationException("该任务定义无效，无法恢复");
        }

        if (!TryGetPausedState(connection, recurringJobId, out var originalCron, out var timeZoneId))
        {
            if (TryResolveCronFromDatabase(recurringJobId, out originalCron, out timeZoneId))
            {
                timeZoneId ??= existing.TimeZoneId;
            }
            else
            {
                throw new InvalidOperationException("该任务未处于暂停状态，或暂停记录已丢失");
            }
        }

        timeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? existing.TimeZoneId : timeZoneId;
        var timeZone = ResolveTimeZone(timeZoneId);

        var manager = new RecurringJobManager();
        manager.AddOrUpdate(
            recurringJobId,
            existing.Job,
            originalCron.Trim(),
            new RecurringJobOptions
            {
                TimeZone = timeZone
            });

        RemovePausedState(connection, recurringJobId);

        await SyncBusinessJobAsync(
            recurringJobId,
            originalCron.Trim(),
            timeZoneId,
            cancellationToken);
    }

    public async Task UpdatePausedCronAsync(
        string recurringJobId,
        string cronExpression,
        CancellationToken cancellationToken = default)
    {
        EnsureJobId(recurringJobId);

        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            throw new InvalidOperationException("Cron 表达式不能为空");
        }

        if (!await IsPausedAsync(recurringJobId, cancellationToken))
        {
            throw new InvalidOperationException("该任务未处于暂停状态");
        }

        using var connection = JobStorage.Current.GetConnection();
        if (!TryGetPausedState(connection, recurringJobId, out _, out var timeZoneId))
        {
            throw new InvalidOperationException("暂停记录已丢失，无法更新 Cron");
        }

        SavePausedState(connection, recurringJobId, cronExpression.Trim(), timeZoneId);
        await SyncBusinessJobAsync(recurringJobId, cronExpression.Trim(), timeZoneId, cancellationToken);
    }

    public async Task UpdatePausedTimeZoneAsync(
        string recurringJobId,
        string timeZoneId,
        CancellationToken cancellationToken = default)
    {
        EnsureJobId(recurringJobId);

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new InvalidOperationException("时区不能为空");
        }

        if (!await IsPausedAsync(recurringJobId, cancellationToken))
        {
            throw new InvalidOperationException("该任务未处于暂停状态");
        }

        using var connection = JobStorage.Current.GetConnection();
        var existing = GetRecurringJobOrThrow(connection, recurringJobId);

        if (existing.Job == null)
        {
            throw new InvalidOperationException("该任务定义无效，无法更新时区");
        }

        if (!TryGetPausedState(connection, recurringJobId, out var originalCron, out _))
        {
            throw new InvalidOperationException("暂停记录已丢失，无法更新时区");
        }

        var normalizedTimeZoneId = timeZoneId.Trim();
        var timeZone = ResolveTimeZone(normalizedTimeZoneId);

        SavePausedState(connection, recurringJobId, originalCron, normalizedTimeZoneId);

        var manager = new RecurringJobManager();
        manager.AddOrUpdate(
            recurringJobId,
            existing.Job,
            Cron.Never(),
            new RecurringJobOptions
            {
                TimeZone = timeZone
            });

        await SyncBusinessJobTimeZoneOnlyAsync(recurringJobId, normalizedTimeZoneId, cancellationToken);
    }

    private async Task SyncBusinessJobTimeZoneOnlyAsync(
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
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<ScheduledJob>>();
        var job = await repository.FindAsync(x => x.Id == jobId, cancellationToken);
        if (job == null)
        {
            return;
        }

        job.TimeZoneId = timeZoneId;
        await repository.UpdateAsync(job, cancellationToken: cancellationToken);
    }

    private static Dictionary<string, string> GetHashEntries(IStorageConnection connection) =>
        connection.GetAllEntriesFromHash(PausedHashKey)
        ?? new Dictionary<string, string>(StringComparer.Ordinal);

    private static bool TryGetPausedState(
        IStorageConnection connection,
        string recurringJobId,
        out string originalCron,
        out string? timeZoneId)
    {
        originalCron = string.Empty;
        timeZoneId = null;

        var entries = GetHashEntries(connection);
        if (!entries.TryGetValue(recurringJobId, out var rawState) ||
            !TryDeserializePausedState(rawState, out originalCron, out timeZoneId))
        {
            originalCron = string.Empty;
            timeZoneId = null;
            return false;
        }

        return true;
    }

    private static void SavePausedState(
        IStorageConnection connection,
        string recurringJobId,
        string cron,
        string? timeZoneId)
    {
        using var transaction = connection.CreateWriteTransaction();
        transaction.SetRangeInHash(
            PausedHashKey,
            new Dictionary<string, string>
            {
                [recurringJobId] = SerializePausedState(cron, timeZoneId)
            });
        transaction.Commit();
    }

    private static void RemovePausedState(IStorageConnection connection, string recurringJobId)
    {
        var entries = GetHashEntries(connection);
        if (!entries.ContainsKey(recurringJobId))
        {
            return;
        }

        var remaining = entries
            .Where(x => !string.Equals(x.Key, recurringJobId, StringComparison.Ordinal))
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);

        using var transaction = connection.CreateWriteTransaction();
        transaction.RemoveHash(PausedHashKey);
        if (remaining.Count > 0)
        {
            transaction.SetRangeInHash(PausedHashKey, remaining);
        }

        transaction.Commit();
    }

    private static string SerializePausedState(string cron, string? timeZoneId) =>
        $"{cron.Trim()}{PausedStateSeparator}{timeZoneId?.Trim() ?? string.Empty}";

    private static bool TryDeserializePausedState(
        string rawState,
        out string cron,
        out string? timeZoneId)
    {
        cron = string.Empty;
        timeZoneId = null;

        if (string.IsNullOrWhiteSpace(rawState))
        {
            return false;
        }

        var separatorIndex = rawState.IndexOf(PausedStateSeparator, StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            cron = rawState.Trim();
            return !string.IsNullOrWhiteSpace(cron);
        }

        cron = rawState[..separatorIndex].Trim();
        var tz = rawState[(separatorIndex + PausedStateSeparator.Length)..].Trim();
        timeZoneId = string.IsNullOrWhiteSpace(tz) ? null : tz;
        return !string.IsNullOrWhiteSpace(cron);
    }

    private bool TryResolveCronFromDatabase(
        string recurringJobId,
        out string cronExpression,
        out string? timeZoneId)
    {
        cronExpression = string.Empty;
        timeZoneId = null;

        const string prefix = "scheduled-job:";
        if (!recurringJobId.StartsWith(prefix, StringComparison.Ordinal) ||
            !long.TryParse(recurringJobId[prefix.Length..], out var jobId))
        {
            return false;
        }

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<ScheduledJob>>();
        var job = repository.FindAsync(x => x.Id == jobId).GetAwaiter().GetResult();
        if (job == null ||
            string.IsNullOrWhiteSpace(job.CronExpression) ||
            string.Equals(job.CronExpression, Cron.Never(), StringComparison.Ordinal))
        {
            return false;
        }

        cronExpression = job.CronExpression;
        timeZoneId = job.TimeZoneId;
        return true;
    }

    private static void EnsureJobId(string recurringJobId)
    {
        if (string.IsNullOrWhiteSpace(recurringJobId))
        {
            throw new InvalidOperationException("任务编号不能为空");
        }
    }

    private static RecurringJobDto GetRecurringJobOrThrow(IStorageConnection connection, string recurringJobId)
    {
        var existing = connection.GetRecurringJobs()
            .FirstOrDefault(x => string.Equals(x.Id, recurringJobId, StringComparison.Ordinal));

        if (existing == null)
        {
            throw new InvalidOperationException($"周期性任务不存在: {recurringJobId}");
        }

        return existing;
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

        if (!long.TryParse(recurringJobId[prefix.Length..], out var jobId))
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
