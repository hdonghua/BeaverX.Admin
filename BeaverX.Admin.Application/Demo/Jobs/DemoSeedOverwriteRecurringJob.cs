using BeaverX.Admin.Application.Contracts.Demo;
using BeaverX.Admin.Application.Contracts.Scheduling;
using BeaverX.Admin.Domain.Shared.Demo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BeaverX.Admin.Application.Demo.Jobs;

public class DemoSeedOverwriteRecurringJob : IRecurringJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDemoModeService _demoModeService;
    private readonly DemoModeOptions _options;
    private readonly ILogger<DemoSeedOverwriteRecurringJob> _logger;

    public DemoSeedOverwriteRecurringJob(
        IServiceScopeFactory scopeFactory,
        IDemoModeService demoModeService,
        IOptions<DemoModeOptions> options,
        ILogger<DemoSeedOverwriteRecurringJob> logger)
    {
        _scopeFactory = scopeFactory;
        _demoModeService = demoModeService;
        _options = options.Value;
        _logger = logger;
    }

    public string CronExpression => BuildCronExpression(_options.ResetIntervalMinutes);

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_demoModeService.IsEnabled)
        {
            return;
        }

        _logger.LogInformation("Demo overwrite recurring job started");

        using var scope = _scopeFactory.CreateScope();
        var overwriteService = scope.ServiceProvider.GetRequiredService<DemoDataOverwriteService>();
        await overwriteService.OverwriteAllAsync(cancellationToken);
    }

    internal static string BuildCronExpression(int resetIntervalMinutes)
    {
        var interval = resetIntervalMinutes <= 0 ? 5 : resetIntervalMinutes;
        return interval >= 60
            ? $"0 */{Math.Max(1, interval / 60)} * * *"
            : $"*/{interval} * * * *";
    }
}
