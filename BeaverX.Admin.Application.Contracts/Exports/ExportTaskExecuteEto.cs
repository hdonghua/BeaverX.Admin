namespace BeaverX.Admin.Application.Contracts.Exports;

public class ExportTaskExecuteEto
{
    public long TaskId { get; set; }

    public string IdempotencyKey { get; set; } = null!;
}
