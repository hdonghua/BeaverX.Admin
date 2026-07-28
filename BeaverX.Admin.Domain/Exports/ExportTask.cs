using BeaverX.Admin.Domain.Shared.Exports;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Domain.Entities;

namespace BeaverX.Admin.Domain.Exports;

public class ExportTask : FullAuditedEntity<Guid>
{
    public Guid UserId { get; set; }
    public string ExportType { get; set; } = null!;
    public string? Parameters { get; set; }
    public string FileName { get; set; } = null!;
    public string? ObjectKey { get; set; }
    public string? FileUrl { get; set; }
    public ExportTaskStatus Status { get; set; } = ExportTaskStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime? CompletedTime { get; set; }
}
