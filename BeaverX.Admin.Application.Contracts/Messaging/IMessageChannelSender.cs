using BeaverX.Admin.Application.Contracts.Messaging.Dtos;

namespace BeaverX.Admin.Application.Contracts.Messaging;

/// <summary>
/// 单一消息渠道发送器（站内信、钉钉、企微等）。
/// </summary>
public interface IMessageChannelSender
{
    string Channel { get; }

    Task<ChannelSendResult> SendAsync(
        MessageDeliveryContext context,
        CancellationToken cancellationToken = default);
}
