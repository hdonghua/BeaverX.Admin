using BeaverX.Admin.Application.Contracts.Storage;
using BeaverX.Admin.Application.Contracts.Storage.Dtos;
using BeaverX.WebMvc.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

public class FileController : BeaverXControllerBase
{
    private readonly IFileAppService _fileAppService;

    public FileController(IFileAppService fileAppService)
    {
        _fileAppService = fileAppService;
    }

    [Authorize]
    [HttpPost("upload")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<FileUploadResultDto> UploadAsync(
        IFormFile file,
        [FromQuery] string? folder,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            throw new StorageException("请选择要上传的文件");
        }

        await using var stream = file.OpenReadStream();
        return await _fileAppService.UploadAsync(
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            folder,
            cancellationToken);
    }

    [Authorize]
    [HttpDelete]
    public Task DeleteAsync([FromQuery] string objectKey, CancellationToken cancellationToken)
        => _fileAppService.DeleteAsync(objectKey, cancellationToken);

    /// <summary>
    /// 通过后端代理访问 MinIO 文件，适用于 img 标签等无法携带 JWT 的场景。
    /// </summary>
    [AllowAnonymous]
    [HttpGet("proxy/{*objectKey}")]
    public async Task<IActionResult> ProxyAsync(string objectKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return BadRequest(new { message = "objectKey 不能为空" });
        }

        try
        {
            var blob = await _fileAppService.GetAsync(objectKey, cancellationToken);
            return File(blob.Content, blob.ContentType, blob.FileName, enableRangeProcessing: true);
        }
        catch (StorageNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
