namespace BeaverX.Admin.Application.Contracts.Storage;

public class MinioOptions
{
    public const string SectionName = "Minio";

    public string Endpoint { get; set; } = "http://127.0.0.1:9000";
    public string AccessKey { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
    public string Bucket { get; set; } = "beaverx-admin";
    public bool UseSsl { get; set; }
    public int MaxFileSizeMb { get; set; } = 10;
    public int PresignedUrlExpirySeconds { get; set; } = 60 * 60 * 24 * 7;
}
