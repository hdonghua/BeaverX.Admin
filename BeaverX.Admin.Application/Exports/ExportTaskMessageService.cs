using BeaverX.Admin.Domain.Exports;
using BeaverX.Admin.Domain.Shared.Exports;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Exports;

/// <summary>
/// 导出任务消费侧编排：认领、重试恢复等，幂等由 export_tasks 状态保证。
/// </summary>
public class ExportTaskMessageService : IScopedDependency
{
    private readonly IRepository<ExportTask> _exportTaskRepository;

    public ExportTaskMessageService(IRepository<ExportTask> exportTaskRepository)
    {
        _exportTaskRepository = exportTaskRepository;
    }

    public async Task<bool> TryClaimForProcessingAsync(
        long taskId,
        CancellationToken cancellationToken = default)
    {
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
            return false;
        }

        var claimed = await _exportTaskRepository.GetQueryable()
            .Where(x => x.Id == taskId && x.Status == ExportTaskStatus.Pending)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.Status, ExportTaskStatus.Processing),
                cancellationToken);

        return claimed > 0;
    }

    public async Task ResetForRetryAsync(long taskId, CancellationToken cancellationToken = default)
    {
        var task = await _exportTaskRepository.FindAsync(x => x.Id == taskId, cancellationToken);
        if (task == null || task.Status != ExportTaskStatus.Processing)
        {
            return;
        }

        task.Status = ExportTaskStatus.Pending;
        await _exportTaskRepository.UpdateAsync(task, cancellationToken: cancellationToken);
    }

    public async Task ResetStuckProcessingAsync(CancellationToken cancellationToken = default)
    {
        await _exportTaskRepository.GetQueryable()
            .Where(x => x.Status == ExportTaskStatus.Processing)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.Status, ExportTaskStatus.Pending),
                cancellationToken);
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
