using BeaverX.Admin.Application.Contracts.Exports;
using BeaverX.Admin.Application.Contracts.Exports.Dtos;
using BeaverX.WebMvc.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

[Authorize]
public class ExportTaskController : BeaverXController
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
        => _exportTaskAppService.GetListAsync(cancellationToken);

    [HttpGet("active-count")]
    public Task<int> GetActiveCountAsync(CancellationToken cancellationToken)
        => _exportTaskAppService.GetActiveCountAsync(cancellationToken);

    [HttpGet("{id:long}/download-url")]
    public Task<ExportDownloadUrlDto> GetDownloadUrlAsync(long id, CancellationToken cancellationToken)
        => _exportTaskAppService.GetDownloadUrlAsync(id, cancellationToken);
}
