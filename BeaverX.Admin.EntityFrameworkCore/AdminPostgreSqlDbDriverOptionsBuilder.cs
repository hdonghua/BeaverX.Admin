using BeaverX.Admin.EntityFrameworkCore.Interceptors;
using BeaverX.EntityFrameworkCore.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.EntityFrameworkCore;

public class AdminPostgreSqlDbDriverOptionsBuilder : IDbDriverOptionsBuilder
{
    public void Configure<TDbContext>(DbContextOptionsBuilder optionsBuilder, string connectionString)
        where TDbContext : DbContext
    {
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
        });

        optionsBuilder.AddInterceptors(new UtcDateTimeSaveChangesInterceptor());
#if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
#endif
    }
}
