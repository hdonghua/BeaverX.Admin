using BeaverX.Admin.Application.Contracts.Storage;
using BeaverX.Admin.Application.Realtime;
using BeaverX.Admin.Domain.Exports;
using BeaverX.Admin.Domain.Shared.Exports;
using BeaverX.Core.Dependency;
using BeaverX.Data.SqlSugar.Repositories;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Exports;

public class ExportTaskExecutor : IScopedDependency
{
    private readonly ISugarRepository<ExportTask> _exportTaskRepository;
    private readonly ExportHandlerRegistry _handlerRegistry;
    private readonly IBlobStorage _blobStorage;
    private readonly RealtimePublisher _realtimePublisher;
    private readonly ILogger<ExportTaskExecutor> _logger;

    public ExportTaskExecutor(
        ISugarRepository<ExportTask> exportTaskRepository,
        ExportHandlerRegistry handlerRegistry,
        IBlobStorage blobStorage,
        RealtimePublisher realtimePublisher,
        ILogger<ExportTaskExecutor> logger)
    {
        _exportTaskRepository = exportTaskRepository;
        _handlerRegistry = handlerRegistry;
        _blobStorage = blobStorage;
        _realtimePublisher = realtimePublisher;
        _logger = logger;
    }

    public async Task ExecuteAsync(long taskId, CancellationToken cancellationToken = default)
    {
        var task = await _exportTaskRepository.FindAsync(x => x.Id == taskId, cancellationToken);
        if (task == null)
        {
            _logger.LogWarning("Export task {TaskId} not found", taskId);
            return;
        }

        if (task.Status is ExportTaskStatus.Completed or ExportTaskStatus.Failed)
        {
            return;
        }

        try
        {
            var handler = _handlerRegistry.GetRequired(task.ExportType);
            var exportResult = await handler.ExportAsync(task.Parameters, cancellationToken);
            try
            {
                var objectKey = BuildObjectKey(task);
                var uploadResult = await _blobStorage.UploadAsync(
                    objectKey,
                    exportResult.Content,
                    exportResult.ContentType,
                    exportResult.Content.Length,
                    cancellationToken: cancellationToken);

                task.ObjectKey = uploadResult.ObjectKey;
                task.FileUrl = uploadResult.Url;
                task.FileName = exportResult.FileName;
            }
            finally
            {
                exportResult.Content.Dispose();
            }

            task.Status = ExportTaskStatus.Completed;
            task.CompletedTime = DateTime.UtcNow;
            task.ErrorMessage = null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Export task {TaskId} failed, will retry via CAP", taskId);
            task.Status = ExportTaskStatus.Pending;
            task.ErrorMessage = ex.Message;
            await _exportTaskRepository.UpdateAsync(task, cancellationToken: cancellationToken);
            await _realtimePublisher.NotifyExportTaskChangedAsync(task, cancellationToken);
            throw;
        }

        await _exportTaskRepository.UpdateAsync(task, cancellationToken: cancellationToken);
        await _realtimePublisher.NotifyExportTaskChangedAsync(task, cancellationToken);
    }

    private static string BuildObjectKey(ExportTask task)
    {
        var safeName = Path.GetFileName(task.FileName);
        return $"exports/{task.UserId}/{task.Id}/{safeName}";
    }
}
