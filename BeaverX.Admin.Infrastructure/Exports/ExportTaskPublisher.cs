using BeaverX.Admin.Application.Contracts.Exports;
using BeaverX.Admin.Application.Exports;
using BeaverX.Admin.Domain.Shared.Exports;
using BeaverX.Core.Dependency;
using DotNetCore.CAP;

namespace BeaverX.Admin.Infrastructure.Exports;

public class ExportTaskPublisher : IExportTaskPublisher, IScopedDependency
{
    private readonly ICapPublisher _capPublisher;
    private readonly ExportTaskMessageService _messageService;

    public ExportTaskPublisher(ICapPublisher capPublisher, ExportTaskMessageService messageService)
    {
        _capPublisher = capPublisher;
        _messageService = messageService;
    }

    public async Task PublishExecuteAsync(long taskId, CancellationToken cancellationToken = default)
    {
        var eto = new ExportTaskExecuteEto
        {
            TaskId = taskId,
            IdempotencyKey = ExportTaskMessageService.BuildIdempotencyKey(taskId)
        };

        await _capPublisher.PublishAsync(
            ExportTaskCapTopics.Execute,
            eto,
            cancellationToken: cancellationToken);

        await _messageService.MarkPublishedAsync(taskId, cancellationToken);
    }
}
