namespace BeaverX.Admin.Application.Contracts.Storage;

public class StorageNotFoundException : StorageException
{
    public StorageNotFoundException(string message) : base(message)
    {
    }
}
