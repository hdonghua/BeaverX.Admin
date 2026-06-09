using BeaverX.Admin.Application.Contracts.Exports;
using BeaverX.Admin.Domain.Shared.Exports;
using BeaverX.Core.Dependency;
using DotNetCore.CAP;

namespace BeaverX.Admin.Infrastructure.Exports;

public class ExportTaskPublisher : IExportTaskPublisher, IScopedDependency
{
    private readonly ICapPublisher _capPublisher;

    public ExportTaskPublisher(ICapPublisher capPublisher)
    {
        _capPublisher = capPublisher;
    }

    public Task PublishExecuteAsync(long taskId, CancellationToken cancellationToken = default) =>
        _capPublisher.PublishAsync(
            ExportTaskCapTopics.Execute,
            new ExportTaskExecuteEto { TaskId = taskId },
            cancellationToken: cancellationToken);
}
