using BeaverX.Admin.Application.Contracts.Messaging;
using BeaverX.Admin.Application.Contracts.Messaging.Dtos;
using BeaverX.Core.Dependency;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Messaging;

public class MessageSender : IMessageSender, IScopedDependency
{
    private readonly MessageChannelRegistry _channelRegistry;
    private readonly ILogger<MessageSender> _logger;

    public MessageSender(
        MessageChannelRegistry channelRegistry,
        ILogger<MessageSender> logger)
    {
        _channelRegistry = channelRegistry;
        _logger = logger;
    }

    public async Task<MessageSendResult> SendAsync(
        SendMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var senders = ResolveSenders(request.Channels);
        if (senders.Count == 0)
        {
            throw new InvalidOperationException("没有可用的消息发送渠道");
        }

        var context = new MessageDeliveryContext { Request = request };
        var results = new Dictionary<string, ChannelSendResult>(StringComparer.OrdinalIgnoreCase);

        foreach (var sender in senders)
        {
            var result = await sender.SendAsync(context, cancellationToken);
            results[sender.Channel] = result;

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Message channel {Channel} failed for user {UserId}: {Error}",
                    sender.Channel,
                    request.UserId,
                    result.ErrorMessage);
            }
        }

        return new MessageSendResult { ChannelResults = results };
    }

    private IReadOnlyList<IMessageChannelSender> ResolveSenders(IReadOnlyList<string>? channels)
    {
        if (channels == null || channels.Count == 0)
        {
            return _channelRegistry.GetAll();
        }

        var senders = new List<IMessageChannelSender>();
        foreach (var channel in channels)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                continue;
            }

            if (_channelRegistry.TryGet(channel, out var sender) && sender != null)
            {
                senders.Add(sender);
                continue;
            }

            _logger.LogWarning("Message channel {Channel} is not registered, skipped", channel);
        }

        return senders;
    }
}
