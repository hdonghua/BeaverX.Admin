namespace BeaverX.Admin.Application.Contracts.Storage.Dtos;

public class FileUploadResultDto
{
    public string Bucket { get; set; } = null!;
    public string ObjectKey { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public long Size { get; set; }
    public string ContentType { get; set; } = null!;
}
