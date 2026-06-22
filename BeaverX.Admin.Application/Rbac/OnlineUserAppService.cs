using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Contracts.Realtime;
using BeaverX.Admin.Application.Realtime;
using BeaverX.Admin.Domain.Shared;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Users;

namespace BeaverX.Admin.Application.Rbac;

public class OnlineUserAppService : IOnlineUserAppService, IScopedDependency
{
    private readonly IOnlineUserTracker _tracker;
    private readonly RealtimePublisher _realtimePublisher;
    private readonly ICurrentUser _currentUser;

    public OnlineUserAppService(
        IOnlineUserTracker tracker,
        RealtimePublisher realtimePublisher,
        ICurrentUser currentUser)
    {
        _tracker = tracker;
        _realtimePublisher = realtimePublisher;
        _currentUser = currentUser;
    }

    public Task<List<OnlineUserDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_tracker.GetOnlineUsers().ToList());
    }

    public async Task KickAsync(long userId, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUser.Id ?? throw new BusinessException("未登录");

        if (currentUserId == userId)
        {
            throw new BusinessException("不能强制下线当前登录账号");
        }

        var onlineUser = _tracker.GetOnlineUsers()
            .FirstOrDefault(x => x.UserId == userId);

        if (onlineUser == null)
        {
            throw new BusinessException("该用户当前不在线");
        }

        await _realtimePublisher.NotifyUserForceOfflineAsync(userId, cancellationToken);
        _tracker.RemoveUserConnections(userId);
        await _realtimePublisher.NotifyOnlineUsersChangedAsync(cancellationToken);
    }
}
