using BeaverX.Admin.Application.Contracts.Messages;
using BeaverX.Admin.Application.Contracts.Messages.Dtos;
using BeaverX.Admin.Application.Realtime;
using BeaverX.Admin.Domain.Messages;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using BeaverX.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Messages;

public class MessageAppService : IMessageAppService, IScopedDependency
{
    private readonly IRepository<UserMessage> _messageRepository;
    private readonly RealtimePublisher _realtimePublisher;
    private readonly ICurrentUser _currentUser;

    public MessageAppService(
        IRepository<UserMessage> messageRepository,
        RealtimePublisher realtimePublisher,
        ICurrentUser currentUser)
    {
        _messageRepository = messageRepository;
        _realtimePublisher = realtimePublisher;
        _currentUser = currentUser;
    }

    public async Task<List<MessageDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var messages = await _messageRepository.GetQueryable()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.IsRead)
            .ThenByDescending(x => x.CreationTime)
            .Take(50)
            .ToListAsync(cancellationToken);

        return messages
            .Select(ToDto)
            .ToList();
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var count = await _messageRepository.GetCountAsync(
            x => x.UserId == userId && !x.IsRead,
            cancellationToken);
        return (int)count;
    }

    public async Task MarkReadAsync(MarkMessagesReadDto input, CancellationToken cancellationToken = default)
    {
        if (input.Ids.Count == 0)
        {
            return;
        }

        var userId = GetCurrentUserId();
        var idSet = input.Ids.ToHashSet();
        var messages = await _messageRepository.GetListAsync(
            x => x.UserId == userId && idSet.Contains(x.Id) && !x.IsRead,
            cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        foreach (var message in messages)
        {
            message.IsRead = true;
        }

        await _messageRepository.UpdateManyAsync(messages, cancellationToken: cancellationToken);
        await _realtimePublisher.NotifyMessageUnreadChangedAsync(userId, cancellationToken);
    }

    public async Task MarkAllReadAsync(MarkAllReadDto input, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var messages = await _messageRepository.GetListAsync(
            x => x.UserId == userId && !x.IsRead &&
                 (string.IsNullOrWhiteSpace(input.Type) || x.Type == input.Type),
            cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        foreach (var message in messages)
        {
            message.IsRead = true;
        }

        await _messageRepository.UpdateManyAsync(messages, cancellationToken: cancellationToken);
        await _realtimePublisher.NotifyMessageUnreadChangedAsync(userId, cancellationToken);
    }

    private long GetCurrentUserId()
        => _currentUser.Id ?? throw new BusinessException("未登录");

    private static MessageDto ToDto(UserMessage message) => new()
    {
        Id = message.Id,
        Type = message.Type,
        Title = message.Title,
        SubTitle = message.SubTitle ?? string.Empty,
        Avatar = message.Avatar,
        Content = message.Content,
        Time = FormatMessageTime(message.CreationTime),
        Status = message.IsRead ? 1 : 0,
        MessageType = message.MessageType
    };

    private static string FormatMessageTime(DateTime creationTime)
    {
        var local = creationTime.Kind == DateTimeKind.Utc
            ? creationTime.ToLocalTime()
            : creationTime;
        var now = DateTime.Now;
        var timePart = local.ToString("HH:mm:ss");

        if (local.Date == now.Date)
        {
            return $"今天 {timePart}";
        }

        if (local.Date == now.Date.AddDays(-1))
        {
            return $"昨天 {timePart}";
        }

        return local.ToString("yyyy-MM-dd HH:mm");
    }
}
