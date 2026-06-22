using BeaverX.Admin.Application.Contracts.Demo;
using BeaverX.Admin.Domain.DataSeeder;
using BeaverX.Core.Dependency;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Demo.Seeders;

public class DemoBusinessDataOverwriteSeeder : IOverwriteDataSeeder, IScopedDependency
{
    private readonly IDemoDatabaseHardResetService _hardResetService;
    private readonly ILogger<DemoBusinessDataOverwriteSeeder> _logger;

    public DemoBusinessDataOverwriteSeeder(
        IDemoDatabaseHardResetService hardResetService,
        ILogger<DemoBusinessDataOverwriteSeeder> logger)
    {
        _hardResetService = hardResetService;
        _logger = logger;
    }

    public int Order => 0;

    public async Task OverwriteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Overwriting demo business data...");
        await _hardResetService.ClearBusinessDemoDataAsync(cancellationToken);
    }
}
