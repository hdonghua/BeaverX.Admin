namespace BeaverX.Admin.Application.Contracts.Storage;

public class BlobObjectResult : IAsyncDisposable
{
    public Stream Content { get; set; } = null!;
    public string ContentType { get; set; } = "application/octet-stream";
    public string FileName { get; set; } = null!;
    public long Size { get; set; }

    public async ValueTask DisposeAsync()
    {
        if (Content != null)
        {
            await Content.DisposeAsync();
        }
    }
}
