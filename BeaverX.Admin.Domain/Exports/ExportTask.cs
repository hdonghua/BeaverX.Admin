using BeaverX.Admin.Domain.Shared.Exports;
using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Exports;

public class ExportTask : FullAuditedEntity
{
    public long UserId { get; set; }
    public string ExportType { get; set; } = null!;
    public string? Parameters { get; set; }
    public string FileName { get; set; } = null!;
    public string? ObjectKey { get; set; }
    public string? FileUrl { get; set; }
    public ExportTaskStatus Status { get; set; } = ExportTaskStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime? CompletedTime { get; set; }
}
