using BeaverX.Admin.Application.Caching;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Rbac;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Rbac;

public class RoleAppService : IRoleAppService, IScopedDependency
{
    private readonly IRepository<Role, Guid> _roleRepository;
    private readonly IRepository<Menu, Guid> _menuRepository;
    private readonly IRepository<RoleMenu, Guid> _roleMenuRepository;
    private readonly IUnitOfWorkManager _unitOfWork;
    private readonly AppCacheInvalidator _cacheInvalidator;

    public RoleAppService(
        IRepository<Role, Guid> roleRepository,
        IRepository<Menu, Guid> menuRepository,
        IRepository<RoleMenu, Guid> roleMenuRepository,
        IUnitOfWorkManager unitOfWork,
        AppCacheInvalidator cacheInvalidator)
    {
        _roleRepository = roleRepository;
        _menuRepository = menuRepository;
        _roleMenuRepository = roleMenuRepository;
        _unitOfWork = unitOfWork;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<PagedResultDto<RoleDto>> GetListAsync(RoleQueryDto input, CancellationToken cancellationToken = default)
    {
        var query = (await _roleRepository.GetQueryableAsync())
            .Include(x => x.RoleMenus)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword));
        }

        if (input.IsEnabled.HasValue)
        {
            query = query.Where(x => x.IsEnabled == input.IsEnabled.Value);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var (skip, take) = RbacQueryHelper.GetPaging(input.Page, input.PageSize);
        var items = await query
            .OrderBy(x => x.Sort)
            .ThenByDescending(x => x.CreationTime)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        var roleDtos = new List<RoleDto>();
        foreach (var item in items)
        {
            roleDtos.Add(await ToRoleDtoAsync(item, cancellationToken));
        }

        return new PagedResultDto<RoleDto>
        {
            Total = total,
            Items = roleDtos
        };
    }

    public async Task<RoleDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await FindRoleWithRelationsAsync(id, cancellationToken);
        return await ToRoleDtoAsync(role, cancellationToken);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleDto input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.Name))
        {
            throw new BusinessException("角色编码和名称不能为空");
        }

        if (await _roleRepository.AnyAsync(x => x.Code == input.Code.Trim(), cancellationToken))
        {
            throw new BusinessException("角色编码已存在");
        }

        var role = new Role
        {
            Code = input.Code.Trim(),
            Name = input.Name.Trim(),
            Description = input.Description,
            Sort = input.Sort,
            IsEnabled = input.IsEnabled
        };

        await _roleRepository.InsertAsync(role, cancellationToken: cancellationToken);
        return RbacMapper.ToRoleDto(role);
    }

    public async Task<RoleDto> UpdateAsync(Guid id, UpdateRoleDto input, CancellationToken cancellationToken = default)
    {
        var role = await FindRoleWithRelationsAsync(id, cancellationToken);

        if (input.Name != null) role.Name = input.Name;
        if (input.Description != null) role.Description = input.Description;
        if (input.Sort.HasValue) role.Sort = input.Sort.Value;
        if (input.IsEnabled.HasValue) role.IsEnabled = input.IsEnabled.Value;

        await _roleRepository.UpdateAsync(role, cancellationToken: cancellationToken);
        if (input.IsEnabled.HasValue)
        {
            await _cacheInvalidator.BumpAccessVersionAsync(cancellationToken);
        }

        return await ToRoleDtoAsync(role, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _roleRepository.DeleteAsync(id, cancellationToken: cancellationToken);
        await _cacheInvalidator.BumpAccessVersionAsync(cancellationToken);
    }

    public async Task AssignMenusAsync(Guid id, AssignRoleMenusDto input, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetAsync(id, cancellationToken: cancellationToken);
        var menuIds = RbacRoleHelper.IsSuperAdminRole(role.Code)
            ? await GetAllMenuIdsAsync(cancellationToken)
            : input.MenuIds;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            await ReplaceRoleMenusAsync(id, menuIds, ct);
        }, cancellationToken);

        await _cacheInvalidator.BumpAccessVersionAsync(cancellationToken);
    }

    private async Task<Role> FindRoleWithRelationsAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await (await _roleRepository.GetQueryableAsync())
            .Include(x => x.RoleMenus)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (role == null)
        {
            throw new BusinessException($"角色不存在: {id}");
        }

        return role;
    }

    private async Task<RoleDto> ToRoleDtoAsync(Role role, CancellationToken cancellationToken)
    {
        var dto = RbacMapper.ToRoleDto(role);
        if (RbacRoleHelper.IsSuperAdminRole(role.Code))
        {
            dto.MenuIds = await GetAllMenuIdsAsync(cancellationToken);
        }

        return dto;
    }

    private async Task<List<Guid>> GetAllMenuIdsAsync(CancellationToken cancellationToken) =>
        await (await _menuRepository.GetQueryableAsync())
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

    private async Task ReplaceRoleMenusAsync(Guid roleId, IEnumerable<Guid> menuIds, CancellationToken cancellationToken)
    {
        var distinctIds = menuIds.Distinct().ToList();
        if (distinctIds.Count > 0)
        {
            var count = await _menuRepository.CountAsync(x => distinctIds.Contains(x.Id), cancellationToken);
            if (count != distinctIds.Count)
            {
                throw new BusinessException("存在无效的菜单 ID");
            }
        }

        await _roleMenuRepository.DeleteAsync(x => x.RoleId == roleId, cancellationToken: cancellationToken);

        if (distinctIds.Count == 0)
        {
            return;
        }

        var items = distinctIds.Select(menuId => new RoleMenu
        {
            RoleId = roleId,
            MenuId = menuId
        });

        await _roleMenuRepository.InsertManyAsync(items, autoSave: true, cancellationToken: cancellationToken);
    }
}
