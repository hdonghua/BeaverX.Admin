using BeaverX.Admin.Domain.Shared.Scheduling;
using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Scheduling;

public class ScheduledJob : FullAuditedEntity
{
    public string JobCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public ScheduledJobType JobType { get; set; } = ScheduledJobType.HttpApi;
    public string CronExpression { get; set; } = null!;
    public string TimeZoneId { get; set; } = "Asia/Shanghai";
    public bool IsEnabled { get; set; } = true;
    public string? Description { get; set; }
    public ScheduledJobHttpMethod HttpMethod { get; set; } = ScheduledJobHttpMethod.Get;
    public string HttpUrl { get; set; } = null!;
    public string? HttpHeadersJson { get; set; }
    public string? HttpBody { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public DateTime? LastRunTime { get; set; }
    public ScheduledJobRunStatus? LastRunStatus { get; set; }
    public string? LastRunMessage { get; set; }
}
