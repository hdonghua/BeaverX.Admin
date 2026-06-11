using BeaverX.Admin.Application.Contracts.Scheduling;
using BeaverX.Core.Dependency;
using Hangfire;

namespace BeaverX.Admin.Infrastructure.Scheduling;

public class HangfireScheduledJobRegistrar : IHangfireScheduledJobRegistrar, ISingletonDependency
{
    public void Register(ScheduledJobRegistration registration)
    {
        var recurringJobId = BuildRecurringJobId(registration.JobId);

        if (!registration.IsEnabled)
        {
            RecurringJob.RemoveIfExists(recurringJobId);
            return;
        }

        var timeZone = ResolveTimeZone(registration.TimeZoneId);
        RecurringJob.AddOrUpdate<HttpApiScheduledJobRunner>(
            recurringJobId,
            runner => runner.ExecuteAsync(registration.JobId, false, CancellationToken.None),
            registration.CronExpression,
            new RecurringJobOptions
            {
                TimeZone = timeZone
            });
    }

    public void Remove(long jobId)
    {
        RecurringJob.RemoveIfExists(BuildRecurringJobId(jobId));
    }

    public string Enqueue(long jobId)
    {
        return BackgroundJob.Enqueue<HttpApiScheduledJobRunner>(
            runner => runner.ExecuteAsync(jobId, true, CancellationToken.None));
    }

    internal static string BuildRecurringJobId(long jobId) => $"scheduled-job:{jobId}";

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Local;
        }
    }
}
