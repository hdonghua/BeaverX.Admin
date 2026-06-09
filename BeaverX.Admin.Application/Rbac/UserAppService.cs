using BeaverX.Admin.Application.Caching;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Realtime;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using BeaverX.Domain.Uow;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Rbac;

public class UserAppService : IUserAppService, IScopedDependency
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<UserRole> _userRoleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AppCacheInvalidator _cacheInvalidator;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly RealtimePublisher _realtimePublisher;

    public UserAppService(
        IRepository<User> userRepository,
        IRepository<Role> roleRepository,
        IRepository<UserRole> userRoleRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        AppCacheInvalidator cacheInvalidator,
        RefreshTokenService refreshTokenService,
        RealtimePublisher realtimePublisher)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _cacheInvalidator = cacheInvalidator;
        _refreshTokenService = refreshTokenService;
        _realtimePublisher = realtimePublisher;
    }

    public async Task<PagedResultDto<UserDto>> GetListAsync(UserQueryDto input, CancellationToken cancellationToken = default)
    {
        var query = _userRepository.GetQueryable()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            query = query.Where(x =>
                x.UserName.Contains(keyword) ||
                (x.NickName != null && x.NickName.Contains(keyword)) ||
                (x.Email != null && x.Email.Contains(keyword)));
        }

        if (input.IsEnabled.HasValue)
        {
            query = query.Where(x => x.IsEnabled == input.IsEnabled.Value);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var (skip, take) = RbacQueryHelper.GetPaging(input.Page, input.PageSize);
        var items = await query
            .OrderByDescending(x => x.CreationTime)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<UserDto>
        {
            Total = total,
            Items = items.Select(x => RbacMapper.ToUserDto(x)).ToList()
        };
    }

    public async Task<UserDto> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var user = await FindUserWithRolesAsync(id, cancellationToken);
        return RbacMapper.ToUserDto(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.UserName) || string.IsNullOrWhiteSpace(input.Password))
        {
            throw new RbacException("用户名和密码不能为空");
        }

        if (await _userRepository.AnyAsync(x => x.UserName == input.UserName.Trim(), cancellationToken))
        {
            throw new RbacException("用户名已存在");
        }

        var user = new User
        {
            UserName = input.UserName.Trim(),
            PasswordHash = _passwordHasher.Hash(input.Password),
            NickName = input.NickName,
            Email = input.Email,
            Phone = input.Phone,
            Avatar = input.Avatar,
            IsEnabled = input.IsEnabled
        };

        await _userRepository.InsertAsync(user, cancellationToken: cancellationToken);
        await AssignRolesAsync(user.Id, new AssignUserRolesDto { RoleIds = input.RoleIds }, cancellationToken);

        return await GetAsync(user.Id, cancellationToken);
    }

    public async Task<UserDto> UpdateAsync(long id, UpdateUserDto input, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetAsync(id, cancellationToken);
        var wasEnabled = user.IsEnabled;

        if (input.NickName != null) user.NickName = input.NickName;
        if (input.Email != null) user.Email = input.Email;
        if (input.Phone != null) user.Phone = input.Phone;
        if (input.Avatar != null) user.Avatar = input.Avatar;
        if (input.IsEnabled.HasValue) user.IsEnabled = input.IsEnabled.Value;

        await _userRepository.UpdateAsync(user, cancellationToken: cancellationToken);
        if (input.IsEnabled.HasValue)
        {
            await _cacheInvalidator.BumpAccessVersionAsync(cancellationToken);

            if (wasEnabled && !user.IsEnabled)
            {
                await _refreshTokenService.RevokeAllForUserAsync(id, cancellationToken);
                await _realtimePublisher.NotifyUserDisabledAsync(id, cancellationToken);
            }
        }

        return await GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        await _userRepository.DeleteAsync(id, cancellationToken: cancellationToken);
    }

    public async Task AssignRolesAsync(long id, AssignUserRolesDto input, CancellationToken cancellationToken = default)
    {
        await _userRepository.GetAsync(id, cancellationToken);
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            await ReplaceUserRolesAsync(id, input.RoleIds, ct);
        }, cancellationToken);

        await _cacheInvalidator.BumpAccessVersionAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(long id, ResetPasswordDto input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.NewPassword))
        {
            throw new RbacException("新密码不能为空");
        }

        var user = await _userRepository.GetAsync(id, cancellationToken);
        user.PasswordHash = _passwordHasher.Hash(input.NewPassword);
        await _userRepository.UpdateAsync(user, cancellationToken: cancellationToken);
    }

    private async Task<User> FindUserWithRolesAsync(long id, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetQueryable()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (user == null)
        {
            throw new RbacException($"用户不存在: {id}");
        }

        return user;
    }

    private async Task ReplaceUserRolesAsync(long userId, IEnumerable<long> roleIds, CancellationToken cancellationToken)
    {
        var distinctRoleIds = roleIds.Distinct().ToList();
        if (distinctRoleIds.Count > 0)
        {
            var existingRoleCount = await _roleRepository.GetCountAsync(x => distinctRoleIds.Contains(x.Id), cancellationToken);
            if (existingRoleCount != distinctRoleIds.Count)
            {
                throw new RbacException("存在无效的角色 ID");
            }
        }

        await _userRoleRepository.DeleteManyAsync(x => x.UserId == userId, cancellationToken);

        if (distinctRoleIds.Count == 0)
        {
            return;
        }

        var userRoles = distinctRoleIds.Select(roleId => new UserRole
        {
            UserId = userId,
            RoleId = roleId
        });

        await _userRoleRepository.InsertManyAsync(userRoles, autoSave: true, cancellationToken: cancellationToken);
    }
}
