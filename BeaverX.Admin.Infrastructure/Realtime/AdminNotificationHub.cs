using System.Security.Claims;
using BeaverX.Admin.Application.Contracts.Realtime;
using BeaverX.Admin.Application.Realtime;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Data.SqlSugar.Repositories;
using BeaverX.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace BeaverX.Admin.Infrastructure.Realtime;

[Authorize]
public class AdminNotificationHub : Hub
{
    private readonly IOnlineUserTracker _tracker;
    private readonly IServiceScopeFactory _scopeFactory;

    public AdminNotificationHub(IOnlineUserTracker tracker, IServiceScopeFactory scopeFactory)
    {
        _tracker = tracker;
        _scopeFactory = scopeFactory;
    }

    public override async Task OnConnectedAsync()
    {
        await RegisterConnectionAsync();
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _tracker.RemoveConnection(Context.ConnectionId);
        await NotifyOnlineUsersChangedAsync();
        await base.OnDisconnectedAsync(exception);
    }

    private async Task RegisterConnectionAsync()
    {
        var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdStr, out var userId))
        {
            return;
        }

        var userName = Context.User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        string? nickName = null;

        using (var scope = _scopeFactory.CreateScope())
        {
            var userRepository = scope.ServiceProvider.GetRequiredService<ISugarRepository<User>>();
            var user = await userRepository.FindAsync(x => x.Id == userId);
            nickName = user?.NickName;
        }

        _tracker.AddConnection(userId, userName, nickName, Context.ConnectionId);
        await NotifyOnlineUsersChangedAsync();
    }

    private async Task NotifyOnlineUsersChangedAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<RealtimePublisher>();
        await publisher.NotifyOnlineUsersChangedAsync();
    }
}
