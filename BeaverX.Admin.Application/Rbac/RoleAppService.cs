using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using BeaverX.Domain.Uow;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Rbac;

public class RoleAppService : IRoleAppService, IScopedDependency
{
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<Menu> _menuRepository;
    private readonly IRepository<RoleMenu> _roleMenuRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RoleAppService(
        IRepository<Role> roleRepository,
        IRepository<Menu> menuRepository,
        IRepository<RoleMenu> roleMenuRepository,
        IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _menuRepository = menuRepository;
        _roleMenuRepository = roleMenuRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResultDto<RoleDto>> GetListAsync(RoleQueryDto input, CancellationToken cancellationToken = default)
    {
        var query = _roleRepository.GetQueryable()
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

    public async Task<RoleDto> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var role = await FindRoleWithRelationsAsync(id, cancellationToken);
        return await ToRoleDtoAsync(role, cancellationToken);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleDto input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.Name))
        {
            throw new RbacException("角色编码和名称不能为空");
        }

        if (await _roleRepository.AnyAsync(x => x.Code == input.Code.Trim(), cancellationToken))
        {
            throw new RbacException("角色编码已存在");
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

    public async Task<RoleDto> UpdateAsync(long id, UpdateRoleDto input, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetAsync(id, cancellationToken);

        if (input.Name != null) role.Name = input.Name;
        if (input.Description != null) role.Description = input.Description;
        if (input.Sort.HasValue) role.Sort = input.Sort.Value;
        if (input.IsEnabled.HasValue) role.IsEnabled = input.IsEnabled.Value;

        await _roleRepository.UpdateAsync(role, cancellationToken: cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        await _roleRepository.DeleteAsync(id, cancellationToken: cancellationToken);
    }

    public async Task AssignMenusAsync(long id, AssignRoleMenusDto input, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetAsync(id, cancellationToken);
        var menuIds = RbacRoleHelper.IsSuperAdminRole(role.Code)
            ? await GetAllMenuIdsAsync(cancellationToken)
            : input.MenuIds;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            await ReplaceRoleMenusAsync(id, menuIds, ct);
        }, cancellationToken);
    }

    private async Task<Role> FindRoleWithRelationsAsync(long id, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetQueryable()
            .Include(x => x.RoleMenus)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (role == null)
        {
            throw new RbacException($"角色不存在: {id}");
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

    private async Task<List<long>> GetAllMenuIdsAsync(CancellationToken cancellationToken) =>
        await _menuRepository.GetQueryable()
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

    private async Task ReplaceRoleMenusAsync(long roleId, IEnumerable<long> menuIds, CancellationToken cancellationToken)
    {
        var distinctIds = menuIds.Distinct().ToList();
        if (distinctIds.Count > 0)
        {
            var count = await _menuRepository.GetCountAsync(x => distinctIds.Contains(x.Id), cancellationToken);
            if (count != distinctIds.Count)
            {
                throw new RbacException("存在无效的菜单 ID");
            }
        }

        await _roleMenuRepository.DeleteManyAsync(x => x.RoleId == roleId, cancellationToken);

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
