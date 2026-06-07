using BeaverX.Admin.Application.Contracts.Storage;
using BeaverX.Core.Dependency;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Microsoft.Extensions.Options;

namespace BeaverX.Admin.Application.Storage;

public class MinioBlobStorage : IBlobStorage, IScopedDependency
{
    private readonly IMinioClient _minioClient;
    private readonly MinioOptions _options;
    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private bool _bucketEnsured;

    public MinioBlobStorage(IMinioClient minioClient, IOptions<MinioOptions> options)
    {
        _minioClient = minioClient;
        _options = options.Value;
    }

    public async Task<BlobUploadResult> UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        long size,
        string? bucket = null,
        CancellationToken cancellationToken = default)
    {
        var bucketName = ResolveBucket(bucket);
        await EnsureBucketAsync(bucketName, cancellationToken);

        try
        {
            var putArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectKey)
                .WithStreamData(content)
                .WithObjectSize(size)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(putArgs, cancellationToken);
            var url = await GetPresignedUrlAsync(objectKey, bucketName, cancellationToken);

            return new BlobUploadResult
            {
                Bucket = bucketName,
                ObjectKey = objectKey,
                Url = url
            };
        }
        catch (MinioException ex)
        {
            throw new StorageException($"文件上传失败: {ex.Message}");
        }
    }

    public async Task DeleteAsync(
        string objectKey,
        string? bucket = null,
        CancellationToken cancellationToken = default)
    {
        var bucketName = ResolveBucket(bucket);

        try
        {
            var removeArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectKey);

            await _minioClient.RemoveObjectAsync(removeArgs, cancellationToken);
        }
        catch (MinioException ex)
        {
            throw new StorageException($"文件删除失败: {ex.Message}");
        }
    }

    public Task<string> GetPresignedUrlAsync(
        string objectKey,
        string? bucket = null,
        CancellationToken cancellationToken = default)
    {
        var bucketName = ResolveBucket(bucket);

        try
        {
            var args = new PresignedGetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectKey)
                .WithExpiry(_options.PresignedUrlExpirySeconds);

            return _minioClient.PresignedGetObjectAsync(args);
        }
        catch (MinioException ex)
        {
            throw new StorageException($"生成访问链接失败: {ex.Message}");
        }
    }

    public async Task<BlobObjectResult> GetAsync(
        string objectKey,
        string? bucket = null,
        CancellationToken cancellationToken = default)
    {
        var bucketName = ResolveBucket(bucket);

        try
        {
            var statArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectKey);
            var stat = await _minioClient.StatObjectAsync(statArgs, cancellationToken);

            var buffer = new MemoryStream();
            var getArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectKey)
                .WithCallbackStream(async (stream, ct) =>
                {
                    await stream.CopyToAsync(buffer, ct);
                });

            await _minioClient.GetObjectAsync(getArgs, cancellationToken);
            buffer.Position = 0;

            return new BlobObjectResult
            {
                Content = buffer,
                ContentType = string.IsNullOrWhiteSpace(stat.ContentType)
                    ? "application/octet-stream"
                    : stat.ContentType,
                FileName = Path.GetFileName(objectKey),
                Size = stat.Size
            };
        }
        catch (MinioException ex) when (IsNotFound(ex))
        {
            throw new StorageNotFoundException("文件不存在");
        }
        catch (MinioException ex)
        {
            throw new StorageException($"文件读取失败: {ex.Message}");
        }
    }

    private static bool IsNotFound(MinioException ex) =>
        ex.Message.Contains("Not Found", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("NoSuchKey", StringComparison.OrdinalIgnoreCase);

    private string ResolveBucket(string? bucket) =>
        string.IsNullOrWhiteSpace(bucket) ? _options.Bucket : bucket.Trim();

    private async Task EnsureBucketAsync(string bucketName, CancellationToken cancellationToken)
    {
        if (_bucketEnsured)
        {
            return;
        }

        await _bucketLock.WaitAsync(cancellationToken);
        try
        {
            if (_bucketEnsured)
            {
                return;
            }

            var existsArgs = new BucketExistsArgs().WithBucket(bucketName);
            var exists = await _minioClient.BucketExistsAsync(existsArgs, cancellationToken);
            if (!exists)
            {
                var makeArgs = new MakeBucketArgs().WithBucket(bucketName);
                await _minioClient.MakeBucketAsync(makeArgs, cancellationToken);
            }

            _bucketEnsured = true;
        }
        finally
        {
            _bucketLock.Release();
        }
    }
}
