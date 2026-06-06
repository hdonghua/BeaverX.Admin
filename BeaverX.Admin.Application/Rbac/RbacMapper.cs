using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Rbac;

namespace BeaverX.Admin.Application.Rbac;

internal static class RbacMapper
{
    public static UserDto ToUserDto(User user, IEnumerable<Role>? roles = null)
    {
        var roleList = roles?.ToList() ?? user.UserRoles.Select(x => x.Role).ToList();
        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            NickName = user.NickName,
            Email = user.Email,
            Phone = user.Phone,
            Avatar = user.Avatar,
            IsEnabled = user.IsEnabled,
            CreationTime = user.CreationTime,
            RoleIds = roleList.Select(x => x.Id).ToList(),
            RoleNames = roleList.Select(x => x.Name).ToList()
        };
    }

    public static RoleDto ToRoleDto(Role role)
    {
        return new RoleDto
        {
            Id = role.Id,
            Code = role.Code,
            Name = role.Name,
            Description = role.Description,
            Sort = role.Sort,
            IsEnabled = role.IsEnabled,
            CreationTime = role.CreationTime,
            PermissionIds = role.RolePermissions.Select(x => x.PermissionId).ToList(),
            MenuIds = role.RoleMenus.Select(x => x.MenuId).ToList()
        };
    }

    public static PermissionDto ToPermissionDto(Permission permission) => new()
    {
        Id = permission.Id,
        ParentId = permission.ParentId,
        Code = permission.Code,
        Name = permission.Name,
        Type = permission.Type,
        Path = permission.Path,
        Method = permission.Method,
        Sort = permission.Sort,
        IsEnabled = permission.IsEnabled
    };

    public static MenuDto ToMenuDto(Menu menu) => new()
    {
        Id = menu.Id,
        ParentId = menu.ParentId,
        Name = menu.Name,
        Path = menu.Path,
        Component = menu.Component,
        Icon = menu.Icon,
        PermissionCode = menu.PermissionCode,
        Sort = menu.Sort,
        IsVisible = menu.IsVisible,
        IsEnabled = menu.IsEnabled
    };
}
