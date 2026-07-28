namespace BeaverX.Admin.Application.Contracts.Exports;

public interface IExportTaskPublisher
{
    Task PublishExecuteAsync(Guid taskId, CancellationToken cancellationToken = default);
}
