using BeaverX.Admin.Application.Contracts.Exports.Dtos;
using BeaverX.Admin.Application.Contracts.Realtime;
using BeaverX.Admin.Application.Contracts.Realtime.Dtos;
using BeaverX.Admin.Domain.Exports;
using BeaverX.Admin.Domain.Messages;
using BeaverX.Admin.Domain.Shared.Exports;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Realtime;

public class RealtimePublisher : IScopedDependency
{
    private readonly IRealtimeNotifier _notifier;
    private readonly IOnlineUserTracker _onlineUserTracker;
    private readonly IRepository<ExportTask, Guid> _exportTaskRepository;
    private readonly IRepository<UserMessage, Guid> _messageRepository;

    public RealtimePublisher(
        IRealtimeNotifier notifier,
        IOnlineUserTracker onlineUserTracker,
        IRepository<ExportTask, Guid> exportTaskRepository,
        IRepository<UserMessage, Guid> messageRepository)
    {
        _notifier = notifier;
        _onlineUserTracker = onlineUserTracker;
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
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var task = await _exportTaskRepository.GetAsync(x => x.Id == taskId, cancellationToken: cancellationToken);
        if (task == null)
        {
            return;
        }

        await NotifyExportTaskChangedAsync(task, cancellationToken);
    }

    public async Task NotifyMessageUnreadChangedAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var unreadCount = await _messageRepository.CountAsync(
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

    public Task NotifyUserDisabledAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _notifier.SendToUserAsync(
            userId,
            RealtimeEvents.UserDisabled,
            new UserDisabledPayload(),
            cancellationToken);

    public Task NotifyUserForceOfflineAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _notifier.SendToUserAsync(
            userId,
            RealtimeEvents.UserForceOffline,
            new UserForceOfflinePayload
            {
                Message = "您已被管理员强制下线"
            },
            cancellationToken);

    public Task NotifyOnlineUsersChangedAsync(CancellationToken cancellationToken = default)
    {
        var users = _onlineUserTracker.GetOnlineUsers().ToList();
        return _notifier.SendToAllAsync(
            RealtimeEvents.OnlineUsersChanged,
            new OnlineUsersChangedPayload
            {
                Users = users,
                TotalConnections = _onlineUserTracker.GetTotalConnectionCount()
            },
            cancellationToken);
    }

    private async Task<int> GetActiveExportCountAsync(Guid userId, CancellationToken cancellationToken)
    {
        var count = await (await _exportTaskRepository.GetQueryableAsync())
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
