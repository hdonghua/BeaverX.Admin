using BeaverX.Admin.Application.Contracts.Storage;
using BeaverX.Admin.Application.Contracts.Storage.Dtos;
using BeaverX.WebMvc.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

public class FileController : BeaverXController
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
}
