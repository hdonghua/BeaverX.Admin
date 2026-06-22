namespace BeaverX.Admin.Domain.DataSeeder;

public interface IOverwriteDataSeeder
{
    int Order { get; }

    Task OverwriteAsync(CancellationToken cancellationToken = default);
}
