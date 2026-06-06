using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;

namespace BeaverX.Admin.Application.Rbac;

public class MenuAppService : IMenuAppService, IScopedDependency
{
    private readonly IRepository<Menu> _menuRepository;

    public MenuAppService(IRepository<Menu> menuRepository)
    {
        _menuRepository = menuRepository;
    }

    public async Task<List<MenuDto>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var menus = await _menuRepository.GetListAsync(cancellationToken);
        var dtos = menus.Select(RbacMapper.ToMenuDto).ToList();
        return RbacQueryHelper.BuildMenuTree(dtos);
    }

    public async Task<MenuDto> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var menu = await _menuRepository.GetAsync(id, cancellationToken);
        return RbacMapper.ToMenuDto(menu);
    }

    public async Task<MenuDto> CreateAsync(CreateMenuDto input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new RbacException("菜单名称不能为空");
        }

        if (input.ParentId.HasValue)
        {
            await _menuRepository.GetAsync(input.ParentId.Value, cancellationToken);
        }

        var menu = new Menu
        {
            ParentId = input.ParentId,
            Name = input.Name.Trim(),
            Path = input.Path,
            Component = input.Component,
            Icon = input.Icon,
            PermissionCode = input.PermissionCode,
            Sort = input.Sort,
            IsVisible = input.IsVisible,
            IsEnabled = input.IsEnabled
        };

        await _menuRepository.InsertAsync(menu, cancellationToken: cancellationToken);
        return RbacMapper.ToMenuDto(menu);
    }

    public async Task<MenuDto> UpdateAsync(long id, UpdateMenuDto input, CancellationToken cancellationToken = default)
    {
        var menu = await _menuRepository.GetAsync(id, cancellationToken);

        if (input.ParentId.HasValue)
        {
            if (input.ParentId.Value == id)
            {
                throw new RbacException("父级菜单不能是自己");
            }

            await _menuRepository.GetAsync(input.ParentId.Value, cancellationToken);
            menu.ParentId = input.ParentId;
        }

        if (input.Name != null) menu.Name = input.Name;
        if (input.Path != null) menu.Path = input.Path;
        if (input.Component != null) menu.Component = input.Component;
        if (input.Icon != null) menu.Icon = input.Icon;
        if (input.PermissionCode != null) menu.PermissionCode = input.PermissionCode;
        if (input.Sort.HasValue) menu.Sort = input.Sort.Value;
        if (input.IsVisible.HasValue) menu.IsVisible = input.IsVisible.Value;
        if (input.IsEnabled.HasValue) menu.IsEnabled = input.IsEnabled.Value;

        await _menuRepository.UpdateAsync(menu, cancellationToken: cancellationToken);
        return RbacMapper.ToMenuDto(menu);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var hasChildren = await _menuRepository.AnyAsync(x => x.ParentId == id, cancellationToken);
        if (hasChildren)
        {
            throw new RbacException("请先删除子菜单");
        }

        await _menuRepository.DeleteAsync(id, cancellationToken: cancellationToken);
    }
}
