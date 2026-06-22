using BeaverX.Admin.Application.Contracts.Demo;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Contracts.Realtime;
using BeaverX.Admin.Application.Realtime;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using BeaverX.Domain.Users;

namespace BeaverX.Admin.Application.Rbac;

public class OnlineUserAppService : IOnlineUserAppService, IScopedDependency
{
    private readonly IOnlineUserTracker _tracker;
    private readonly RealtimePublisher _realtimePublisher;
    private readonly ICurrentUser _currentUser;
    private readonly IRepository<User> _userRepository;
    private readonly IDemoModeService _demoModeService;

    public OnlineUserAppService(
        IOnlineUserTracker tracker,
        RealtimePublisher realtimePublisher,
        ICurrentUser currentUser,
        IRepository<User> userRepository,
        IDemoModeService demoModeService)
    {
        _tracker = tracker;
        _realtimePublisher = realtimePublisher;
        _currentUser = currentUser;
        _userRepository = userRepository;
        _demoModeService = demoModeService;
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

        var user = await _userRepository.FindAsync(x => x.Id == userId, cancellationToken);
        if (user != null)
        {
            _demoModeService.EnsureAdminUserOperable(user.UserName);
        }

        await _realtimePublisher.NotifyUserForceOfflineAsync(userId, cancellationToken);
        _tracker.RemoveUserConnections(userId);
        await _realtimePublisher.NotifyOnlineUsersChangedAsync(cancellationToken);
    }
}
