using BeaverX.Admin.Application.Contracts.Storage.Dtos;

namespace BeaverX.Admin.Application.Contracts.Storage;

public interface IFileAppService
{
    Task<FileUploadResultDto> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        long size,
        string? folder = null,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);
}
