using BeaverX.Admin.Application.Contracts.Messaging;
using BeaverX.Admin.Application.Contracts.Messaging.Dtos;
using BeaverX.Admin.Application.Realtime;
using BeaverX.Admin.Domain.Messages;
using BeaverX.Admin.Domain.Shared.Messaging;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Messages;

/// <summary>
/// 站内信渠道：写入 user_messages 并推送未读数变更。
/// </summary>
public class SiteMessageChannelSender : IMessageChannelSender, IScopedDependency
{
    private readonly IRepository<UserMessage> _messageRepository;
    private readonly RealtimePublisher _realtimePublisher;
    private readonly ILogger<SiteMessageChannelSender> _logger;

    public SiteMessageChannelSender(
        IRepository<UserMessage> messageRepository,
        RealtimePublisher realtimePublisher,
        ILogger<SiteMessageChannelSender> logger)
    {
        _messageRepository = messageRepository;
        _realtimePublisher = realtimePublisher;
        _logger = logger;
    }

    public string Channel => MessageChannels.Site;

    public async Task<ChannelSendResult> SendAsync(
        MessageDeliveryContext context,
        CancellationToken cancellationToken = default)
    {
        var request = context.Request;

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return ChannelSendResult.Fail("消息标题不能为空");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return ChannelSendResult.Fail("消息内容不能为空");
        }

        try
        {
            var message = new UserMessage
            {
                UserId = request.UserId,
                Type = request.Type,
                Title = request.Title,
                SubTitle = request.SubTitle,
                Avatar = request.Avatar,
                Content = request.Content,
                MessageType = request.MessageType,
                IsRead = false
            };

            await _messageRepository.InsertAsync(message, cancellationToken: cancellationToken);
            await _realtimePublisher.NotifyMessageUnreadChangedAsync(request.UserId, cancellationToken);

            return ChannelSendResult.Ok(message.Id.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send site message to user {UserId}", request.UserId);
            return ChannelSendResult.Fail(ex.Message);
        }
    }
}
