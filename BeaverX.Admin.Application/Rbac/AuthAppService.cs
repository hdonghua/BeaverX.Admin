using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using BeaverX.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Rbac;

public class AuthAppService : IAuthAppService, IScopedDependency
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Menu> _menuRepository;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ICurrentUser _currentUser;

    public AuthAppService(
        IRepository<User> userRepository,
        IRepository<Menu> menuRepository,
        JwtTokenService jwtTokenService,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _menuRepository = menuRepository;
        _jwtTokenService = jwtTokenService;
        _currentUser = currentUser;
    }

    public async Task<LoginResultDto> LoginAsync(LoginDto input, CancellationToken cancellationToken = default)
    {
        var user = await LoadUserWithAccessAsync(input.UserName.Trim(), cancellationToken);

        if (user == null || !user.IsEnabled || !PasswordHasher.Verify(input.Password, user.PasswordHash))
        {
            throw new RbacException("用户名或密码错误");
        }

        var roles = GetRoleCodes(user);
        var permissions = await ResolvePermissionsAsync(roles, user, cancellationToken);
        var (token, expiresIn) = _jwtTokenService.CreateToken(user.Id, user.UserName, roles, permissions);

        return new LoginResultDto
        {
            Token = token,
            ExpiresIn = expiresIn,
            User = BuildProfile(user, roles, permissions)
        };
    }

    public async Task<UserProfileDto> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.Id ?? throw new RbacException("未登录");
        var user = await LoadUserWithAccessByIdAsync(userId, cancellationToken)
            ?? throw new RbacException("用户不存在");

        var roles = GetRoleCodes(user);
        var permissions = await ResolvePermissionsAsync(roles, user, cancellationToken);
        return BuildProfile(user, roles, permissions);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(
        UpdateProfileDto input,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.Id ?? throw new RbacException("未登录");
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
        if (string.IsNullOrWhiteSpace(input.OldPassword) ||
            string.IsNullOrWhiteSpace(input.NewPassword))
        {
            throw new RbacException("原密码和新密码不能为空");
        }

        var userId = _currentUser.Id ?? throw new RbacException("未登录");
        var user = await _userRepository.GetAsync(userId, cancellationToken);

        if (!PasswordHasher.Verify(input.OldPassword, user.PasswordHash))
        {
            throw new RbacException("原密码错误");
        }

        user.PasswordHash = PasswordHasher.Hash(input.NewPassword);
        await _userRepository.UpdateAsync(user, cancellationToken: cancellationToken);
    }

    public async Task<List<MenuDto>> GetCurrentUserMenusAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.Id ?? throw new RbacException("未登录");
        var user = await LoadUserWithAccessByIdAsync(userId, cancellationToken)
            ?? throw new RbacException("用户不存在");

        var roles = GetRoleCodes(user);
        var isSuperAdmin = IsSuperAdmin(roles);
        var allMenus = await _menuRepository.GetListAsync(cancellationToken);
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

    private async Task<List<string>> ResolvePermissionsAsync(
        List<string> roles,
        User user,
        CancellationToken cancellationToken)
    {
        if (IsSuperAdmin(roles))
        {
            var allMenus = await _menuRepository.GetListAsync(cancellationToken);
            return RbacMenuHelper.CollectPerms(allMenus);
        }

        var roleMenuIds = user.UserRoles
            .SelectMany(x => x.Role.RoleMenus)
            .Select(x => x.MenuId)
            .ToHashSet();

        if (roleMenuIds.Count == 0)
        {
            return [];
        }

        var menus = await _menuRepository.GetListAsync(x => roleMenuIds.Contains(x.Id), cancellationToken);
        return RbacMenuHelper.CollectPerms(menus);
    }

    private static bool IsSuperAdmin(IEnumerable<string> roles) =>
        roles.Contains(RbacPermissionCodes.SuperAdmin, StringComparer.OrdinalIgnoreCase);

    private static string? NormalizeOptionalString(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static UserProfileDto BuildProfile(User user, List<string> roles, List<string> permissions) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        NickName = user.NickName,
        Email = user.Email,
        Phone = user.Phone,
        Avatar = user.Avatar,
        Roles = roles,
        Permissions = permissions
    };
}
