using BeaverX.Admin.Domain.Shared.Exports;

namespace BeaverX.Admin.Application.Contracts.Exports.Dtos;

public class ExportTaskDto
{
    public long Id { get; set; }
    public string ExportType { get; set; } = null!;
    public string? Parameters { get; set; }
    public string FileName { get; set; } = null!;
    public string? FileUrl { get; set; }
    public ExportTaskStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? CompletedTime { get; set; }
}

public class CreateExportTaskDto
{
    public string ExportType { get; set; } = null!;
    public object? Parameters { get; set; }
}

public class ExportDownloadUrlDto
{
    public string Url { get; set; } = null!;
    public string FileName { get; set; } = null!;
}
