using BeaverX.Admin.Domain.Shared.Scheduling;
using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Scheduling;

public class ScheduledJobLog : CreationAuditedEntity
{
    public long JobId { get; set; }
    public ScheduledJobRunStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public int? DurationMs { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsManualTrigger { get; set; }

    public ScheduledJob Job { get; set; } = null!;
}
