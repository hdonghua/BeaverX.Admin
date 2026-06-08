using BeaverX.Admin.Application.Contracts.Messaging;
using BeaverX.Core.Dependency;

namespace BeaverX.Admin.Application.Messaging;

public class MessageChannelRegistry : IScopedDependency
{
    private readonly IReadOnlyDictionary<string, IMessageChannelSender> _senders;

    public MessageChannelRegistry(IEnumerable<IMessageChannelSender> senders)
    {
        _senders = senders.ToDictionary(x => x.Channel, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<IMessageChannelSender> GetAll() => _senders.Values.ToList();

    public IMessageChannelSender GetRequired(string channel)
    {
        if (!_senders.TryGetValue(channel, out var sender))
        {
            throw new InvalidOperationException($"未注册的消息渠道: {channel}");
        }

        return sender;
    }

    public bool TryGet(string channel, out IMessageChannelSender? sender) =>
        _senders.TryGetValue(channel, out sender);
}
