using BeaverX.Admin.Application.Caching;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;

namespace BeaverX.Admin.Application.Rbac;

public class MenuAppService : IMenuAppService, IScopedDependency
{
    private readonly IRepository<Menu> _menuRepository;
    private readonly MenuCacheService _menuCacheService;
    private readonly AppCacheInvalidator _cacheInvalidator;

    public MenuAppService(
        IRepository<Menu> menuRepository,
        MenuCacheService menuCacheService,
        AppCacheInvalidator cacheInvalidator)
    {
        _menuRepository = menuRepository;
        _menuCacheService = menuCacheService;
        _cacheInvalidator = cacheInvalidator;
    }

    public Task<List<MenuDto>> GetTreeAsync(CancellationToken cancellationToken = default) =>
        _menuCacheService.GetMenuTreeAsync(cancellationToken);

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

        await ValidatePermsAsync(input.Perms, null, cancellationToken);

        var menu = new Menu
        {
            ParentId = input.ParentId,
            Name = input.Name.Trim(),
            MenuType = input.MenuType,
            Perms = string.IsNullOrWhiteSpace(input.Perms) ? null : input.Perms.Trim(),
            Path = MenuInputValidator.NormalizePath(input.Path, input.IsExternal),
            Component = MenuInputValidator.NormalizeComponent(input.Component, input.IsExternal),
            Icon = string.IsNullOrWhiteSpace(input.Icon) ? null : input.Icon,
            Sort = input.Sort,
            IsVisible = input.IsVisible,
            IsEnabled = input.IsEnabled,
            IsExternal = input.IsExternal
        };

        MenuInputValidator.Sanitize(menu);
        MenuInputValidator.Validate(menu.MenuType, menu.Path, menu.Component, menu.IsExternal);

        await _menuRepository.InsertAsync(menu, cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateMenusAsync(cancellationToken);
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
        if (input.MenuType.HasValue) menu.MenuType = input.MenuType.Value;
        if (input.Perms != null)
        {
            menu.Perms = string.IsNullOrWhiteSpace(input.Perms) ? null : input.Perms.Trim();
            await ValidatePermsAsync(menu.Perms, id, cancellationToken);
        }
        if (input.IsExternal.HasValue)
        {
            menu.IsExternal = input.IsExternal.Value;
        }

        if (input.Path != null)
        {
            menu.Path = MenuInputValidator.NormalizePath(input.Path, menu.IsExternal);
        }

        if (input.Component != null)
        {
            menu.Component = MenuInputValidator.NormalizeComponent(input.Component, menu.IsExternal);
        }

        if (input.Icon != null)
        {
            menu.Icon = string.IsNullOrWhiteSpace(input.Icon) ? null : input.Icon;
        }
        if (input.Sort.HasValue) menu.Sort = input.Sort.Value;
        if (input.IsVisible.HasValue) menu.IsVisible = input.IsVisible.Value;
        if (input.IsEnabled.HasValue) menu.IsEnabled = input.IsEnabled.Value;

        MenuInputValidator.Sanitize(menu);
        MenuInputValidator.Validate(menu.MenuType, menu.Path, menu.Component, menu.IsExternal);

        await _menuRepository.UpdateAsync(menu, cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateMenusAsync(cancellationToken);
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
        await _cacheInvalidator.InvalidateMenusAsync(cancellationToken);
    }

    private async Task ValidatePermsAsync(string? perms, long? excludeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(perms))
        {
            return;
        }

        var exists = await _menuRepository.AnyAsync(
            x => x.Perms == perms && (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);

        if (exists)
        {
            throw new RbacException($"权限标识已存在: {perms}");
        }
    }
}
