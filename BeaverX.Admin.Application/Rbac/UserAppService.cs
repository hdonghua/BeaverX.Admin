using BeaverX.Admin.Application.Caching;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Realtime;
using BeaverX.Admin.Domain.Rbac;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Rbac;

public class UserAppService : IUserAppService, IScopedDependency
{
    private readonly IRepository<User, Guid> _userRepository;
    private readonly IRepository<Role, Guid> _roleRepository;
    private readonly IRepository<UserRole, Guid> _userRoleRepository;
    private readonly IUnitOfWorkManager _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AppCacheInvalidator _cacheInvalidator;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly RealtimePublisher _realtimePublisher;

    public UserAppService(
        IRepository<User, Guid> userRepository,
        IRepository<Role, Guid> roleRepository,
        IRepository<UserRole, Guid> userRoleRepository,
        IUnitOfWorkManager unitOfWork,
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
        var query = (await _userRepository.GetQueryableAsync())
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

    public async Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await FindUserWithRolesAsync(id, cancellationToken);
        return RbacMapper.ToUserDto(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.UserName))
        {
            throw new BusinessException("用户名不能为空");
        }

        PasswordInputValidator.Validate(input.Password);

        if (await _userRepository.AnyAsync(x => x.UserName == input.UserName.Trim(), cancellationToken))
        {
            throw new BusinessException("用户名已存在");
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

        return RbacMapper.ToUserDto(user);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserDto input, CancellationToken cancellationToken = default)
    {
        var user = await FindUserWithRolesAsync(id, cancellationToken);
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

        return RbacMapper.ToUserDto(user);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _userRepository.DeleteAsync(id, cancellationToken: cancellationToken);
    }

    public async Task AssignRolesAsync(Guid id, AssignUserRolesDto input, CancellationToken cancellationToken = default)
    {
        await _userRepository.GetAsync(id, cancellationToken: cancellationToken);
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            await ReplaceUserRolesAsync(id, input.RoleIds, ct);
        }, cancellationToken);

        await _cacheInvalidator.BumpAccessVersionAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(Guid id, ResetPasswordDto input, CancellationToken cancellationToken = default)
    {
        PasswordInputValidator.Validate(input.NewPassword);

        var user = await _userRepository.GetAsync(id, cancellationToken: cancellationToken);
        user.PasswordHash = _passwordHasher.Hash(input.NewPassword);
        await _userRepository.UpdateAsync(user, cancellationToken: cancellationToken);
    }

    private async Task<User> FindUserWithRolesAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await (await _userRepository.GetQueryableAsync())
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (user == null)
        {
            throw new BusinessException($"用户不存在: {id}");
        }

        return user;
    }

    private async Task ReplaceUserRolesAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken cancellationToken)
    {
        var distinctRoleIds = roleIds.Distinct().ToList();
        if (distinctRoleIds.Count > 0)
        {
            var existingRoleCount = await _roleRepository.CountAsync(x => distinctRoleIds.Contains(x.Id), cancellationToken);
            if (existingRoleCount != distinctRoleIds.Count)
            {
                throw new BusinessException("存在无效的角色 ID");
            }
        }

        await _userRoleRepository.DeleteAsync(x => x.UserId == userId, cancellationToken: cancellationToken);

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
