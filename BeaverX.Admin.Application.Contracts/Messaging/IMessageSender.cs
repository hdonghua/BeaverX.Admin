using BeaverX.Admin.Application.Contracts.Messaging.Dtos;

namespace BeaverX.Admin.Application.Contracts.Messaging;

/// <summary>
/// 通用消息发送门面，按渠道分发到各 <see cref="IMessageChannelSender"/> 实现。
/// </summary>
public interface IMessageSender
{
    Task<MessageSendResult> SendAsync(
        SendMessageRequest request,
        CancellationToken cancellationToken = default);
}
