using BeaverX.Admin.Application.Contracts.Realtime;
using BeaverX.Core.Dependency;
using Microsoft.AspNetCore.SignalR;

namespace BeaverX.Admin.Infrastructure.Realtime;

public class SignalRRealtimeNotifier : IRealtimeNotifier, IScopedDependency
{
    private readonly IHubContext<AdminNotificationHub> _hubContext;

    public SignalRRealtimeNotifier(IHubContext<AdminNotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task SendToUserAsync(
        long userId,
        string eventName,
        object? payload,
        CancellationToken cancellationToken = default)
    {
        var message = new RealtimeMessage
        {
            Event = eventName,
            Data = payload
        };

        return _hubContext.Clients
            .User(userId.ToString())
            .SendAsync("Receive", message, cancellationToken);
    }
}
