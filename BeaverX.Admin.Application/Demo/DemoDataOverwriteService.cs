using BeaverX.Admin.Application.Caching;
using BeaverX.Admin.Application.Contracts.Demo;
using BeaverX.Admin.Domain.DataSeeder;
using BeaverX.Core.Dependency;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Demo;

public class DemoDataOverwriteService : ISingletonDependency
{
    private readonly IDemoModeService _demoModeService;
    private readonly IEnumerable<IOverwriteDataSeeder> _seeders;
    private readonly AppCacheInvalidator _cacheInvalidator;
    private readonly ILogger<DemoDataOverwriteService> _logger;

    public DemoDataOverwriteService(
        IDemoModeService demoModeService,
        IEnumerable<IOverwriteDataSeeder> seeders,
        AppCacheInvalidator cacheInvalidator,
        ILogger<DemoDataOverwriteService> logger)
    {
        _demoModeService = demoModeService;
        _seeders = seeders;
        _cacheInvalidator = cacheInvalidator;
        _logger = logger;
    }

    public async Task OverwriteAllAsync(CancellationToken cancellationToken = default)
    {
        if (!_demoModeService.IsEnabled)
        {
            return;
        }

        _logger.LogInformation("Demo mode overwrite started");

        foreach (var seeder in _seeders.OrderBy(x => x.Order))
        {
            _logger.LogInformation(
                "Running demo overwrite seeder {Seeder}",
                seeder.GetType().Name);

            await seeder.OverwriteAsync(cancellationToken);
        }

        await _cacheInvalidator.InvalidateMenusAsync(cancellationToken);
        await _cacheInvalidator.BumpAccessVersionAsync(cancellationToken);

        _logger.LogInformation("Demo mode overwrite completed");
    }
}
