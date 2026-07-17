using BeaverX.Admin.Domain.Shared.Scheduling;
using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Scheduling;

[SugarTable("sys_scheduled_job_logs")]
public class ScheduledJobLog : CreationAuditedEntity
{
    public long JobId { get; set; }
    public ScheduledJobRunStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public int? DurationMs { get; set; }
    public int? HttpStatusCode { get; set; }

    [SugarColumn(ColumnDataType = "text")]
    public string? ResponseBody { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsManualTrigger { get; set; }

    [Navigate(NavigateType.OneToOne, nameof(JobId))]
    public ScheduledJob Job { get; set; } = null!;
}
