using BeaverX.Admin.Application.Demo;
using BeaverX.Admin.Application.Contracts.Demo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Demo;

public class DemoModeStartupHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDemoModeService _demoModeService;
    private readonly ILogger<DemoModeStartupHostedService> _logger;

    public DemoModeStartupHostedService(
        IServiceScopeFactory scopeFactory,
        IDemoModeService demoModeService,
        ILogger<DemoModeStartupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _demoModeService = demoModeService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_demoModeService.IsEnabled)
        {
            return;
        }

        _logger.LogInformation("Demo mode enabled, running initial overwrite...");

        using var scope = _scopeFactory.CreateScope();
        var overwriteService = scope.ServiceProvider.GetRequiredService<DemoDataOverwriteService>();
        await overwriteService.OverwriteAllAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
