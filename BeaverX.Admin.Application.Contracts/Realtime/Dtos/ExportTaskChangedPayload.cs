using BeaverX.Admin.Application.Contracts.Exports.Dtos;

namespace BeaverX.Admin.Application.Contracts.Realtime.Dtos;

public class ExportTaskChangedPayload
{
    public ExportTaskDto Task { get; set; } = null!;
    public int ActiveCount { get; set; }
}
