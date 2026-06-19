using BeaverX.Admin.Application.Caching;
using BeaverX.Admin.Application.Contracts.Caching;
using BeaverX.Admin.Domain.Shared;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using BeaverX.Domain.Uow;
using BeaverX.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Rbac;

public class AuthAppService : IAuthAppService, IScopedDependency
{
    private readonly IRepository<User> _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly MenuCacheService _menuCacheService;
    private readonly IUserPermissionResolver _userPermissionResolver;
    private readonly ICacheService _cache;
    private readonly AppCacheInvalidator _cacheInvalidator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public AuthAppService(
        IRepository<User> userRepository,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher,
        RefreshTokenService refreshTokenService,
        MenuCacheService menuCacheService,
        IUserPermissionResolver userPermissionResolver,
        ICacheService cache,
        AppCacheInvalidator cacheInvalidator,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _refreshTokenService = refreshTokenService;
        _menuCacheService = menuCacheService;
        _userPermissionResolver = userPermissionResolver;
        _cache = cache;
        _cacheInvalidator = cacheInvalidator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<LoginResultDto> LoginAsync(LoginDto input, CancellationToken cancellationToken = default)
    {
        var user = await LoadUserWithAccessAsync(input.UserName.Trim(), cancellationToken);

        if (user == null || !user.IsEnabled || !_passwordHasher.Verify(input.Password, user.PasswordHash))
        {
            throw new BusinessException("用户名或密码错误");
        }

        var roles = GetRoleCodes(user);
        var permissions = await _userPermissionResolver.GetPermissionsAsync(user.Id, cancellationToken);
        var tokenResult = await IssueTokensAsync(user, roles, cancellationToken);

        return new LoginResultDto
        {
            Token = tokenResult.Token,
            RefreshToken = tokenResult.RefreshToken,
            ExpiresIn = tokenResult.ExpiresIn,
            RefreshExpiresIn = tokenResult.RefreshExpiresIn,
            User = BuildProfile(user, roles, permissions)
        };
    }

    public async Task<TokenResultDto> RefreshTokenAsync(
        RefreshTokenDto input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.RefreshToken))
        {
            throw new BusinessException("刷新令牌不能为空");
        }

        TokenResultDto? result = null;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var plainToken = input.RefreshToken.Trim();
            var userId = await _refreshTokenService.TryConsumeAsync(plainToken, cancellationToken: ct);
            if (userId == null)
            {
                throw new BusinessException("刷新令牌无效、已过期或已被使用");
            }

            var user = await LoadUserWithAccessByIdAsync(userId.Value, ct)
                ?? throw new BusinessException("用户不存在");

            if (!user.IsEnabled)
            {
                throw new BusinessException("用户已被禁用");
            }

            var roles = GetRoleCodes(user);
            result = await IssueTokensAsync(user, roles, ct);
        }, cancellationToken);

        return result!;
    }

    public async Task LogoutAsync(RefreshTokenDto? input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input?.RefreshToken))
        {
            return;
        }

        await _refreshTokenService.TryConsumeAsync(
            input.RefreshToken.Trim(),
            cancellationToken: cancellationToken);
    }

    public async Task<UserProfileDto> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.Id ?? throw new BusinessException("未登录");
        var user = await LoadUserWithAccessByIdAsync(userId, cancellationToken)
            ?? throw new BusinessException("用户不存在");

        var roles = GetRoleCodes(user);
        var permissions = await _userPermissionResolver.GetPermissionsAsync(user.Id, cancellationToken);
        return BuildProfile(user, roles, permissions);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(
        UpdateProfileDto input,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.Id ?? throw new BusinessException("未登录");
        var user = await _userRepository.GetAsync(userId, cancellationToken);

        if (input.NickName != null)
        {
            user.NickName = input.NickName.Trim();
        }

        if (input.Email != null)
        {
            user.Email = NormalizeOptionalString(input.Email);
        }

        if (input.Phone != null)
        {
            user.Phone = NormalizeOptionalString(input.Phone);
        }

        if (input.Avatar != null)
        {
            user.Avatar = NormalizeOptionalString(input.Avatar);
        }

        await _userRepository.UpdateAsync(user, cancellationToken: cancellationToken);
        return await GetProfileAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(
        ChangePasswordDto input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.OldPassword))
        {
            throw new BusinessException("原密码不能为空");
        }

        PasswordInputValidator.Validate(input.NewPassword);

        var userId = _currentUser.Id ?? throw new BusinessException("未登录");
        var user = await _userRepository.GetAsync(userId, cancellationToken);

        if (!_passwordHasher.Verify(input.OldPassword, user.PasswordHash))
        {
            throw new BusinessException("原密码错误");
        }

        user.PasswordHash = _passwordHasher.Hash(input.NewPassword);
        await _userRepository.UpdateAsync(user, cancellationToken: cancellationToken);
    }

    public async Task<List<MenuDto>> GetCurrentUserMenusAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.Id ?? throw new BusinessException("未登录");
        var user = await LoadUserWithAccessByIdAsync(userId, cancellationToken)
            ?? throw new BusinessException("用户不存在");

        if (!user.IsEnabled)
        {
            throw new BusinessException("用户已被禁用");
        }

        var accessVersion = await _cacheInvalidator.GetAccessVersionAsync(cancellationToken);
        var cacheKey = CacheKeys.UserMenus(userId, accessVersion);

        return await _cache.GetOrSetAsync(
            cacheKey,
            async ct => await BuildCurrentUserMenusAsync(user, ct),
            CacheDurations.UserAccess,
            cancellationToken);
    }

    private async Task<List<MenuDto>> BuildCurrentUserMenusAsync(User user, CancellationToken cancellationToken)
    {
        var roles = GetRoleCodes(user);
        var isSuperAdmin = IsSuperAdmin(roles);
        var allMenus = await _menuCacheService.GetAllMenusAsync(cancellationToken);
        var roleMenuIds = user.UserRoles
            .SelectMany(x => x.Role.RoleMenus)
            .Select(x => x.MenuId)
            .ToHashSet();

        var routerMenus = RbacMenuHelper.FilterRouters(allMenus, roleMenuIds, isSuperAdmin);
        var dtos = routerMenus.Select(RbacMapper.ToMenuDto).ToList();
        return RbacMenuHelper.ToRouterTree(dtos);
    }

    private async Task<User?> LoadUserWithAccessAsync(string userName, CancellationToken cancellationToken)
    {
        return await _userRepository.GetQueryable()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .ThenInclude(x => x.RoleMenus)
            .FirstOrDefaultAsync(x => x.UserName == userName, cancellationToken);
    }

    private async Task<User?> LoadUserWithAccessByIdAsync(long userId, CancellationToken cancellationToken)
    {
        return await _userRepository.GetQueryable()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .ThenInclude(x => x.RoleMenus)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    private static List<string> GetRoleCodes(User user) =>
        user.UserRoles
            .Where(x => x.Role.IsEnabled)
            .Select(x => x.Role.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static bool IsSuperAdmin(IEnumerable<string> roles) =>
        roles.Contains(RbacPermissionCodes.SuperAdmin, StringComparer.OrdinalIgnoreCase);

    private static string? NormalizeOptionalString(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<TokenResultDto> IssueTokensAsync(
        User user,
        List<string> roles,
        CancellationToken cancellationToken)
    {
        var (accessToken, expiresIn) = _jwtTokenService.CreateToken(
            user.Id,
            user.UserName,
            roles);
        var (refreshToken, expiresAt) = await _refreshTokenService.CreateAsync(
            user.Id,
            cancellationToken);

        return new TokenResultDto
        {
            Token = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn,
            RefreshExpiresIn = (int)Math.Max(
                0,
                (expiresAt - DateTime.UtcNow).TotalSeconds)
        };
    }

    private static UserProfileDto BuildProfile(
        User user,
        List<string> roles,
        IReadOnlyCollection<string> permissions) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        NickName = user.NickName,
        Email = user.Email,
        Phone = user.Phone,
        Avatar = user.Avatar,
        Roles = roles,
        Permissions = permissions.ToList()
    };
}
