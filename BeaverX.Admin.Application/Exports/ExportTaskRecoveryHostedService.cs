using BeaverX.Core.Dependency;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Exports;

/// <summary>
/// 启动时恢复中断的导出任务，并重新发布 CAP 消息。
/// </summary>
public class ExportTaskRecoveryHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExportTaskRecoveryHostedService> _logger;

    public ExportTaskRecoveryHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExportTaskRecoveryHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var messageService = scope.ServiceProvider.GetRequiredService<ExportTaskMessageService>();
        var publisher = scope.ServiceProvider.GetRequiredService<ExportTaskPublisher>();

        await messageService.ResetStuckProcessingAsync(cancellationToken);

        var taskIds = await messageService.GetRepublishTaskIdsAsync(cancellationToken);
        if (taskIds.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Recovering {Count} pending export tasks via CAP", taskIds.Count);
        foreach (var taskId in taskIds)
        {
            await publisher.PublishExecuteAsync(taskId, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
