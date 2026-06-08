using BeaverX.Admin.Application.Messaging;
using BeaverX.Admin.Domain.Exports;
using BeaverX.Admin.Domain.Shared.Exports;
using BeaverX.Admin.Domain.Shared.Messaging;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
namespace BeaverX.Admin.Application.Exports;

/// <summary>
/// 导出任务与通用本地消息表的编排层。
/// </summary>
public class ExportTaskMessageService : IScopedDependency
{
    private readonly IRepository<ExportTask> _exportTaskRepository;
    private readonly LocalMessageOutboxService _outboxService;

    public ExportTaskMessageService(
        IRepository<ExportTask> exportTaskRepository,
        LocalMessageOutboxService outboxService)
    {
        _exportTaskRepository = exportTaskRepository;
        _outboxService = outboxService;
    }

    public static string BuildIdempotencyKey(long taskId) =>
        LocalMessageOutboxService.BuildIdempotencyKey(
            LocalMessageTypes.ExportTaskExecute,
            taskId.ToString());

    public Task CreateOutboxAsync(long taskId, string? payload = null, CancellationToken cancellationToken = default) =>
        _outboxService.CreateAsync(
            LocalMessageTypes.ExportTaskExecute,
            taskId.ToString(),
            payload,
            cancellationToken);

    public Task MarkPublishedAsync(long taskId, CancellationToken cancellationToken = default) =>
        _outboxService.MarkPublishedAsync(BuildIdempotencyKey(taskId), cancellationToken);

    public async Task<bool> TryClaimForProcessingAsync(
        long taskId,
        string capMessageId,
        CancellationToken cancellationToken = default)
    {
        var idempotencyKey = BuildIdempotencyKey(taskId);
        if (await _outboxService.IsConsumedAsync(idempotencyKey, cancellationToken))
        {
            return false;
        }

        var task = await _exportTaskRepository.FindAsync(x => x.Id == taskId, cancellationToken);
        if (task == null)
        {
            return false;
        }

        if (task.Status is ExportTaskStatus.Completed or ExportTaskStatus.Failed)
        {
            return false;
        }

        if (task.Status == ExportTaskStatus.Processing)
        {
            return await _outboxService.TryBindConsumeMessageAsync(
                idempotencyKey,
                capMessageId,
                cancellationToken);
        }

        var claimed = await _exportTaskRepository.GetQueryable()
            .Where(x => x.Id == taskId && x.Status == ExportTaskStatus.Pending)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.Status, ExportTaskStatus.Processing),
                cancellationToken);

        if (claimed == 0)
        {
            return false;
        }

        if (!await _outboxService.TryBindConsumeMessageAsync(
                idempotencyKey,
                capMessageId,
                cancellationToken))
        {
            task.Status = ExportTaskStatus.Pending;
            await _exportTaskRepository.UpdateAsync(task, cancellationToken: cancellationToken);
            return false;
        }

        return true;
    }

    public Task ResetForRetryAsync(long taskId, CancellationToken cancellationToken = default) =>
        _outboxService.ResetConsumeLockAsync(BuildIdempotencyKey(taskId), cancellationToken);

    public Task MarkConsumedAsync(long taskId, CancellationToken cancellationToken = default) =>
        _outboxService.MarkConsumedAsync(BuildIdempotencyKey(taskId), cancellationToken);

    public async Task ResetStuckProcessingAsync(CancellationToken cancellationToken = default)
    {
        var stuckTaskIds = await _exportTaskRepository.GetQueryable()
            .Where(x => x.Status == ExportTaskStatus.Processing)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var taskId in stuckTaskIds)
        {
            var task = await _exportTaskRepository.FindAsync(x => x.Id == taskId, cancellationToken);
            if (task == null)
            {
                continue;
            }

            task.Status = ExportTaskStatus.Pending;
            await _exportTaskRepository.UpdateAsync(task, cancellationToken: cancellationToken);
            await _outboxService.ResetConsumeLockAsync(BuildIdempotencyKey(taskId), cancellationToken);
        }
    }

    public async Task<List<long>> GetRepublishTaskIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _exportTaskRepository.GetQueryable()
            .Where(x => x.Status == ExportTaskStatus.Pending)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }
}
