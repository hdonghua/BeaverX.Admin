namespace BeaverX.Admin.Application.Contracts.Scheduling;

public interface IHangfireScheduledJobRegistrar
{
    void Register(ScheduledJobRegistration registration);

    void Remove(Guid jobId);

    string Enqueue(Guid jobId);
}

public sealed class ScheduledJobRegistration
{
    public required Guid JobId { get; init; }
    public required string CronExpression { get; init; }
    public required string TimeZoneId { get; init; }
    public required bool IsEnabled { get; init; }
}
