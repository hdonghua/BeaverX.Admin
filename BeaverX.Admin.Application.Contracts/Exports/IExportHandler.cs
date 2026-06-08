namespace BeaverX.Admin.Application.Contracts.Exports;

public interface IExportHandler
{
    string ExportType { get; }

    string DisplayName { get; }

    Task<ExportHandlerResult> ExportAsync(
        string? parametersJson,
        CancellationToken cancellationToken = default);
}

public class ExportHandlerResult
{
    public Stream Content { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
