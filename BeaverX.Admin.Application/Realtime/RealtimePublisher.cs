using BeaverX.Admin.Application.Contracts.Exports.Dtos;
using BeaverX.Admin.Application.Contracts.Realtime;
using BeaverX.Admin.Application.Contracts.Realtime.Dtos;
using BeaverX.Admin.Domain.Exports;
using BeaverX.Admin.Domain.Messages;
using BeaverX.Admin.Domain.Shared.Exports;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Realtime;

public class RealtimePublisher : IScopedDependency
{
    private readonly IRealtimeNotifier _notifier;
    private readonly IRepository<ExportTask> _exportTaskRepository;
    private readonly IRepository<UserMessage> _messageRepository;

    public RealtimePublisher(
        IRealtimeNotifier notifier,
        IRepository<ExportTask> exportTaskRepository,
        IRepository<UserMessage> messageRepository)
    {
        _notifier = notifier;
        _exportTaskRepository = exportTaskRepository;
        _messageRepository = messageRepository;
    }

    public async Task NotifyExportTaskChangedAsync(
        ExportTask task,
        CancellationToken cancellationToken = default)
    {
        var activeCount = await GetActiveExportCountAsync(task.UserId, cancellationToken);
        await _notifier.SendToUserAsync(
            task.UserId,
            RealtimeEvents.ExportTaskChanged,
            new ExportTaskChangedPayload
            {
                Task = ToExportTaskDto(task),
                ActiveCount = activeCount
            },
            cancellationToken);
    }

    public async Task NotifyExportTaskChangedByIdAsync(
        long taskId,
        CancellationToken cancellationToken = default)
    {
        var task = await _exportTaskRepository.FindAsync(x => x.Id == taskId, cancellationToken);
        if (task == null)
        {
            return;
        }

        await NotifyExportTaskChangedAsync(task, cancellationToken);
    }

    public async Task NotifyMessageUnreadChangedAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        var unreadCount = await _messageRepository.GetCountAsync(
            x => x.UserId == userId && !x.IsRead,
            cancellationToken);

        await _notifier.SendToUserAsync(
            userId,
            RealtimeEvents.MessageUnreadChanged,
            new MessageUnreadChangedPayload
            {
                UnreadCount = (int)unreadCount
            },
            cancellationToken);
    }

    private async Task<int> GetActiveExportCountAsync(long userId, CancellationToken cancellationToken)
    {
        var count = await _exportTaskRepository.GetQueryable()
            .LongCountAsync(
                x => x.UserId == userId &&
                     (x.Status == ExportTaskStatus.Pending || x.Status == ExportTaskStatus.Processing),
                cancellationToken);

        return (int)count;
    }

    private static ExportTaskDto ToExportTaskDto(ExportTask entity) => new()
    {
        Id = entity.Id,
        ExportType = entity.ExportType,
        Parameters = entity.Parameters,
        FileName = entity.FileName,
        FileUrl = entity.FileUrl,
        Status = entity.Status,
        ErrorMessage = entity.ErrorMessage,
        CreationTime = entity.CreationTime,
        CompletedTime = entity.CompletedTime
    };
}
