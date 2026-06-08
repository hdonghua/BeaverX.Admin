namespace BeaverX.Admin.Application.Contracts.Realtime;

public interface IRealtimeNotifier
{
    Task SendToUserAsync(
        long userId,
        string eventName,
        object? payload,
        CancellationToken cancellationToken = default);
}
