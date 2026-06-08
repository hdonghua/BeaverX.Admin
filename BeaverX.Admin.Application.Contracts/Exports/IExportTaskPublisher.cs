namespace BeaverX.Admin.Application.Contracts.Exports;

public interface IExportTaskPublisher
{
    Task PublishExecuteAsync(long taskId, CancellationToken cancellationToken = default);
}
