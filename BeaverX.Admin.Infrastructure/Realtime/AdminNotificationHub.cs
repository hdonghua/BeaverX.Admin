using System.Security.Claims;
using BeaverX.Admin.Application.Contracts.Realtime;
using BeaverX.Admin.Application.Realtime;
using BeaverX.Admin.Domain.Rbac;
using Volo.Abp.Domain.Repositories;
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

    /// <summary>
    /// 前端定时心跳，续期 Redis 在线 TTL；无心跳约 90s 后视为离线。
    /// </summary>
    public Task Heartbeat()
    {
        _tracker.TouchConnection(Context.ConnectionId);
        return Task.CompletedTask;
    }

    private async Task RegisterConnectionAsync()
    {
        var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return;
        }

        var userName = Context.User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        string? nickName = null;

        await _scopeFactory.RunInUnitOfWorkAsync(async (sp, _) =>
        {
            var userRepository = sp.GetRequiredService<IRepository<User, Guid>>();
            var user = await userRepository.FindAsync(x => x.Id == userId);
            nickName = user?.NickName;
        });

        var deviceId = ResolveDeviceId();
        _tracker.AddConnection(userId, userName, nickName, Context.ConnectionId, deviceId);
        await NotifyOnlineUsersChangedAsync();
    }

    private string ResolveDeviceId()
    {
        var http = Context.GetHttpContext();
        var deviceId = http?.Request.Query["deviceId"].FirstOrDefault()?.Trim();
        if (!string.IsNullOrWhiteSpace(deviceId) && deviceId.Length <= 128)
        {
            return deviceId;
        }

        // 未传指纹时退化为按连接计，避免阻断实时通道
        return Context.ConnectionId;
    }

    private async Task NotifyOnlineUsersChangedAsync()
    {
        await _scopeFactory.RunInUnitOfWorkAsync(async (sp, _) =>
        {
            var publisher = sp.GetRequiredService<RealtimePublisher>();
            await publisher.NotifyOnlineUsersChangedAsync();
        });
    }
}
