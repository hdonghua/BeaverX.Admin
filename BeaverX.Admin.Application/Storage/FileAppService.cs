using BeaverX.Admin.Application.Contracts.Storage;
using BeaverX.Admin.Application.Contracts.Storage.Dtos;
using BeaverX.Core.Dependency;
using Microsoft.Extensions.Options;

namespace BeaverX.Admin.Application.Storage;

public class FileAppService : IFileAppService, IScopedDependency
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".txt", ".csv", ".zip", ".rar", ".7z"
    };

    private readonly IBlobStorage _blobStorage;
    private readonly MinioOptions _options;

    public FileAppService(IBlobStorage blobStorage, IOptions<MinioOptions> options)
    {
        _blobStorage = blobStorage;
        _options = options.Value;
    }

    public async Task<FileUploadResultDto> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        long size,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        if (size <= 0)
        {
            throw new StorageException("文件不能为空");
        }

        var maxBytes = _options.MaxFileSizeMb * 1024L * 1024L;
        if (size > maxBytes)
        {
            throw new StorageException($"文件大小不能超过 {_options.MaxFileSizeMb}MB");
        }

        var safeFileName = Path.GetFileName(fileName.Trim());
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            throw new StorageException("文件名无效");
        }

        var extension = Path.GetExtension(safeFileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new StorageException("不支持的文件类型");
        }

        var objectKey = BuildObjectKey(safeFileName, folder);
        var result = await _blobStorage.UploadAsync(
            objectKey,
            content,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            size,
            cancellationToken: cancellationToken);

        return new FileUploadResultDto
        {
            Bucket = result.Bucket,
            ObjectKey = result.ObjectKey,
            Url = result.Url,
            FileName = safeFileName,
            Size = size,
            ContentType = contentType
        };
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            throw new StorageException("objectKey 不能为空");
        }

        return _blobStorage.DeleteAsync(objectKey.Trim(), cancellationToken: cancellationToken);
    }

    private static string BuildObjectKey(string fileName, string? folder)
    {
        var extension = Path.GetExtension(fileName);
        var datePrefix = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var uniqueName = $"{Guid.NewGuid():N}{extension}";

        if (string.IsNullOrWhiteSpace(folder))
        {
            return $"{datePrefix}/{uniqueName}";
        }

        var normalizedFolder = folder.Trim().Trim('/').Replace('\\', '/');
        return $"{normalizedFolder}/{datePrefix}/{uniqueName}";
    }
}
