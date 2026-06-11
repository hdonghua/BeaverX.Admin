namespace BeaverX.Admin.Application.Contracts.Scheduling;

public interface IHangfireScheduledJobRegistrar
{
    void Register(ScheduledJobRegistration registration);

    void Remove(long jobId);

    string Enqueue(long jobId);
}

public sealed class ScheduledJobRegistration
{
    public required long JobId { get; init; }
    public required string CronExpression { get; init; }
    public required string TimeZoneId { get; init; }
    public required bool IsEnabled { get; init; }
}
