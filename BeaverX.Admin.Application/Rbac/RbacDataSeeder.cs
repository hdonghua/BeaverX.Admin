using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace BeaverX.Admin.Application.Rbac;

public class RbacDataSeeder : IScopedDependency
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<Permission> _permissionRepository;
    private readonly IRepository<Menu> _menuRepository;
    private readonly IRepository<UserRole> _userRoleRepository;
    private readonly IRepository<RolePermission> _rolePermissionRepository;
    private readonly IRepository<RoleMenu> _roleMenuRepository;
    private readonly ILogger<RbacDataSeeder> _logger;

    public RbacDataSeeder(
        IRepository<User> userRepository,
        IRepository<Role> roleRepository,
        IRepository<Permission> permissionRepository,
        IRepository<Menu> menuRepository,
        IRepository<UserRole> userRoleRepository,
        IRepository<RolePermission> rolePermissionRepository,
        IRepository<RoleMenu> roleMenuRepository,
        ILogger<RbacDataSeeder> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _menuRepository = menuRepository;
        _userRoleRepository = userRoleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _roleMenuRepository = roleMenuRepository;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _userRepository.AnyAsync(_ => true, cancellationToken))
        {
            return;
        }

        _logger.LogInformation("Seeding RBAC initial data...");

        var permissions = CreateDefaultPermissions();
        await _permissionRepository.InsertManyAsync(permissions, cancellationToken: cancellationToken);

        var systemMenu = new Menu
        {
            Name = "系统管理",
            Path = "/system",
            Icon = "setting",
            Sort = 1,
            IsVisible = true
        };
        await _menuRepository.InsertAsync(systemMenu, cancellationToken: cancellationToken);

        var childMenus = new List<Menu>
        {
            new() { ParentId = systemMenu.Id, Name = "用户管理", Path = "/system/users", Component = "system/users/index", Icon = "user", PermissionCode = RbacPermissionCodes.System.User.List, Sort = 1 },
            new() { ParentId = systemMenu.Id, Name = "角色管理", Path = "/system/roles", Component = "system/roles/index", Icon = "team", PermissionCode = RbacPermissionCodes.System.Role.List, Sort = 2 },
            new() { ParentId = systemMenu.Id, Name = "权限管理", Path = "/system/permissions", Component = "system/permissions/index", Icon = "lock", PermissionCode = RbacPermissionCodes.System.Permission.List, Sort = 3 },
            new() { ParentId = systemMenu.Id, Name = "菜单管理", Path = "/system/menus", Component = "system/menus/index", Icon = "menu", PermissionCode = RbacPermissionCodes.System.Menu.List, Sort = 4 }
        };
        await _menuRepository.InsertManyAsync(childMenus, cancellationToken: cancellationToken);

        var allMenus = new List<Menu> { systemMenu };
        allMenus.AddRange(childMenus);

        var adminRole = new Role
        {
            Code = RbacPermissionCodes.SuperAdmin,
            Name = "超级管理员",
            Description = "拥有系统全部权限",
            Sort = 0,
            IsEnabled = true
        };
        await _roleRepository.InsertAsync(adminRole, cancellationToken: cancellationToken);

        await _rolePermissionRepository.InsertManyAsync(
            permissions.Select(permission => new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = permission.Id
            }),
            cancellationToken: cancellationToken);

        await _roleMenuRepository.InsertManyAsync(
            allMenus.Select(menu => new RoleMenu
            {
                RoleId = adminRole.Id,
                MenuId = menu.Id
            }),
            cancellationToken: cancellationToken);

        var adminUser = new User
        {
            UserName = "admin",
            PasswordHash = PasswordHasher.Hash("admin123"),
            NickName = "系统管理员",
            IsEnabled = true
        };
        await _userRepository.InsertAsync(adminUser, cancellationToken: cancellationToken);

        await _userRoleRepository.InsertAsync(new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        }, cancellationToken: cancellationToken);

        _logger.LogInformation("RBAC initial data seeded. Default account: admin / admin123");
    }

    private static List<Permission> CreateDefaultPermissions() =>
    [
        new Permission { Code = RbacPermissionCodes.System.User.List, Name = "用户列表", Type = PermissionType.Api, Path = "/api/User/list", Method = "GET", Sort = 1 },
        new Permission { Code = RbacPermissionCodes.System.User.Create, Name = "创建用户", Type = PermissionType.Api, Path = "/api/User", Method = "POST", Sort = 2 },
        new Permission { Code = RbacPermissionCodes.System.User.Update, Name = "更新用户", Type = PermissionType.Api, Path = "/api/User/{id}", Method = "PUT", Sort = 3 },
        new Permission { Code = RbacPermissionCodes.System.User.Delete, Name = "删除用户", Type = PermissionType.Api, Path = "/api/User/{id}", Method = "DELETE", Sort = 4 },
        new Permission { Code = RbacPermissionCodes.System.User.AssignRoles, Name = "分配角色", Type = PermissionType.Api, Path = "/api/User/{id}/roles", Method = "PUT", Sort = 5 },
        new Permission { Code = RbacPermissionCodes.System.User.ResetPassword, Name = "重置密码", Type = PermissionType.Api, Path = "/api/User/{id}/password", Method = "PUT", Sort = 6 },
        new Permission { Code = RbacPermissionCodes.System.Role.List, Name = "角色列表", Type = PermissionType.Api, Path = "/api/Role/list", Method = "GET", Sort = 10 },
        new Permission { Code = RbacPermissionCodes.System.Role.Create, Name = "创建角色", Type = PermissionType.Api, Path = "/api/Role", Method = "POST", Sort = 11 },
        new Permission { Code = RbacPermissionCodes.System.Role.Update, Name = "更新角色", Type = PermissionType.Api, Path = "/api/Role/{id}", Method = "PUT", Sort = 12 },
        new Permission { Code = RbacPermissionCodes.System.Role.Delete, Name = "删除角色", Type = PermissionType.Api, Path = "/api/Role/{id}", Method = "DELETE", Sort = 13 },
        new Permission { Code = RbacPermissionCodes.System.Role.AssignPermissions, Name = "分配权限", Type = PermissionType.Api, Path = "/api/Role/{id}/permissions", Method = "PUT", Sort = 14 },
        new Permission { Code = RbacPermissionCodes.System.Role.AssignMenus, Name = "分配菜单", Type = PermissionType.Api, Path = "/api/Role/{id}/menus", Method = "PUT", Sort = 15 },
        new Permission { Code = RbacPermissionCodes.System.Permission.List, Name = "权限树", Type = PermissionType.Api, Path = "/api/Permission/tree", Method = "GET", Sort = 20 },
        new Permission { Code = RbacPermissionCodes.System.Permission.Create, Name = "创建权限", Type = PermissionType.Api, Path = "/api/Permission", Method = "POST", Sort = 21 },
        new Permission { Code = RbacPermissionCodes.System.Permission.Update, Name = "更新权限", Type = PermissionType.Api, Path = "/api/Permission/{id}", Method = "PUT", Sort = 22 },
        new Permission { Code = RbacPermissionCodes.System.Permission.Delete, Name = "删除权限", Type = PermissionType.Api, Path = "/api/Permission/{id}", Method = "DELETE", Sort = 23 },
        new Permission { Code = RbacPermissionCodes.System.Menu.List, Name = "菜单树", Type = PermissionType.Api, Path = "/api/Menu/tree", Method = "GET", Sort = 30 },
        new Permission { Code = RbacPermissionCodes.System.Menu.Create, Name = "创建菜单", Type = PermissionType.Api, Path = "/api/Menu", Method = "POST", Sort = 31 },
        new Permission { Code = RbacPermissionCodes.System.Menu.Update, Name = "更新菜单", Type = PermissionType.Api, Path = "/api/Menu/{id}", Method = "PUT", Sort = 32 },
        new Permission { Code = RbacPermissionCodes.System.Menu.Delete, Name = "删除菜单", Type = PermissionType.Api, Path = "/api/Menu/{id}", Method = "DELETE", Sort = 33 }
    ];
}
