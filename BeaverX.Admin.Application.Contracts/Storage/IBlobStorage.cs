namespace BeaverX.Admin.Application.Contracts.Storage;

public interface IBlobStorage
{
    Task<BlobUploadResult> UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        long size,
        string? bucket = null,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string objectKey,
        string? bucket = null,
        CancellationToken cancellationToken = default);

    Task<string> GetPresignedUrlAsync(
        string objectKey,
        string? bucket = null,
        CancellationToken cancellationToken = default);

    Task<BlobObjectResult> GetAsync(
        string objectKey,
        string? bucket = null,
        CancellationToken cancellationToken = default);
}

public class BlobUploadResult
{
    public string Bucket { get; set; } = null!;
    public string ObjectKey { get; set; } = null!;
    public string Url { get; set; } = null!;
}
