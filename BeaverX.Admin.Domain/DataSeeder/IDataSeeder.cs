namespace BeaverX.Admin.Domain.DataSeeder
{
    public interface IDataSeeder
    {
        Task SeedAsync(CancellationToken cancellationToken = default);
    }
}
