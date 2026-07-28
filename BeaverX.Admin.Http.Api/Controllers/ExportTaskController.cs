using BeaverX.Admin.Application.Contracts.Exports;
using BeaverX.Admin.Application.Contracts.Exports.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

[Authorize]
public class ExportTaskController : AdminControllerBase
{
    private readonly IExportTaskAppService _exportTaskAppService;

    public ExportTaskController(IExportTaskAppService exportTaskAppService)
    {
        _exportTaskAppService = exportTaskAppService;
    }

    [HttpPost]
    public Task<ExportTaskDto> CreateAsync(
        [FromBody] CreateExportTaskDto input,
        CancellationToken cancellationToken)
        => _exportTaskAppService.CreateAsync(input, cancellationToken);

    [HttpGet("list")]
    public Task<List<ExportTaskDto>> GetListAsync(CancellationToken cancellationToken)
        => _exportTaskAppService.GetListAsync(cancellationToken: cancellationToken);

    [HttpGet("active-count")]
    public Task<int> GetActiveCountAsync(CancellationToken cancellationToken)
        => _exportTaskAppService.GetActiveCountAsync(cancellationToken);

    [HttpGet("{id:guid}/download-url")]
    public Task<ExportDownloadUrlDto> GetDownloadUrlAsync(Guid id, CancellationToken cancellationToken)
        => _exportTaskAppService.GetDownloadUrlAsync(id, cancellationToken);
}
