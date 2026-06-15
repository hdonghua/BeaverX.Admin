using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Shared.Scheduling;

namespace BeaverX.Admin.Application.Contracts.Scheduling.Dtos;

public class ScheduledJobDto
{
    public long Id { get; set; }
    public string JobCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public ScheduledJobType JobType { get; set; }
    public string CronExpression { get; set; } = null!;
    public string TimeZoneId { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public string? Description { get; set; }
    public ScheduledJobHttpMethod HttpMethod { get; set; }
    public string HttpUrl { get; set; } = null!;
    public string? HttpHeadersJson { get; set; }
    public string? HttpBody { get; set; }
    public int TimeoutSeconds { get; set; }
    public DateTime? LastRunTime { get; set; }
    public ScheduledJobRunStatus? LastRunStatus { get; set; }
    public string? LastRunMessage { get; set; }
    public DateTime CreationTime { get; set; }
}

public class ScheduledJobLogDto
{
    public long Id { get; set; }
    public long JobId { get; set; }
    public ScheduledJobRunStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public int? DurationMs { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsManualTrigger { get; set; }
}

public class ScheduledJobQueryDto : PagedQueryDto
{
    public string? Keyword { get; set; }
    public bool? IsEnabled { get; set; }
}

public class ScheduledJobLogQueryDto : PagedQueryDto
{
}

public class CreateScheduledJobDto
{
    public string JobCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public ScheduledJobType JobType { get; set; } = ScheduledJobType.HttpApi;
    public string CronExpression { get; set; } = null!;
    public string? TimeZoneId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Description { get; set; }
    public ScheduledJobHttpMethod HttpMethod { get; set; } = ScheduledJobHttpMethod.Get;
    public string HttpUrl { get; set; } = null!;
    public string? HttpHeadersJson { get; set; }
    public string? HttpBody { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}

public class UpdateScheduledJobDto
{
    public string? Name { get; set; }
    public string? CronExpression { get; set; }
    public string? TimeZoneId { get; set; }
    public bool? IsEnabled { get; set; }
    public string? Description { get; set; }
    public ScheduledJobHttpMethod? HttpMethod { get; set; }
    public string? HttpUrl { get; set; }
    public string? HttpHeadersJson { get; set; }
    public string? HttpBody { get; set; }
    public int? TimeoutSeconds { get; set; }
}

public class ValidateCronDto
{
    public string CronExpression { get; set; } = null!;
}

public class ValidateCronResultDto
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public List<DateTime>? NextOccurrences { get; set; }
}
