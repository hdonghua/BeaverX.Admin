using BeaverX.Admin.Application.Contracts.Exports.Dtos;

namespace BeaverX.Admin.Application.Contracts.Exports;

public interface IExportTaskAppService
{
    Task<ExportTaskDto> CreateAsync(
        CreateExportTaskDto input,
        CancellationToken cancellationToken = default);

    Task<List<ExportTaskDto>> GetListAsync(CancellationToken cancellationToken = default);

    Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default);

    Task<ExportDownloadUrlDto> GetDownloadUrlAsync(
        long id,
        CancellationToken cancellationToken = default);
}
