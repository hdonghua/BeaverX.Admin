using BeaverX.Admin.EntityFrameworkCore.Interceptors;
using BeaverX.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BeaverX.Admin.EntityFrameworkCore;

public class AdminDbContextFactory : IDesignTimeDbContextFactory<AdminDbContext>
{
    public AdminDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found.");

        var optionsBuilder = new DbContextOptionsBuilder<AdminDbContext>();
        optionsBuilder.UseMySQL(connectionString, mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
        });
        optionsBuilder.AddInterceptors(new UtcDateTimeSaveChangesInterceptor());

        return new AdminDbContext(optionsBuilder.Options, DesignTimeCurrentUser.Instance);
    }

    private static IConfiguration BuildConfiguration()
    {
        var hostProjectPath = ResolveHostProjectPath();

        return new ConfigurationBuilder()
            .SetBasePath(hostProjectPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();
    }

    private static string ResolveHostProjectPath()
    {
        var candidates = new[]
        {
            Directory.GetCurrentDirectory(),
            Path.Combine(Directory.GetCurrentDirectory(), "../BeaverX.Admin.Http.Host"),
            Path.Combine(Directory.GetCurrentDirectory(), "../../BeaverX.Admin.Http.Host"),
            Path.Combine(Directory.GetCurrentDirectory(), "../../../BeaverX.Admin.Http.Host")
        };

        foreach (var candidate in candidates)
        {
            var fullPath = Path.GetFullPath(candidate);
            if (File.Exists(Path.Combine(fullPath, "appsettings.json")))
            {
                return fullPath;
            }
        }

        throw new InvalidOperationException(
            "Could not find BeaverX.Admin.Http.Host/appsettings.json. " +
            "Run migrations with --startup-project BeaverX.Admin.Http.Host.");
    }

    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        public static readonly DesignTimeCurrentUser Instance = new();

        public long? Id => null;
        public string? UserName => null;
    }
}
