using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BeaverX.Admin.Application.Rbac;

public class PermissionAppService : IPermissionAppService, IScopedDependency
{
    private readonly IRepository<Permission> _permissionRepository;

    public PermissionAppService(IRepository<Permission> permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    public async Task<List<PermissionDto>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionRepository.GetListAsync(cancellationToken);
        var dtos = permissions.Select(RbacMapper.ToPermissionDto).ToList();
        return RbacQueryHelper.BuildPermissionTree(dtos);
    }

    public async Task<PermissionDto> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var permission = await _permissionRepository.GetAsync(id, cancellationToken);
        return RbacMapper.ToPermissionDto(permission);
    }

    public async Task<PermissionDto> CreateAsync(CreatePermissionDto input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.Name))
        {
            throw new RbacException("权限编码和名称不能为空");
        }

        if (await _permissionRepository.AnyAsync(x => x.Code == input.Code.Trim(), cancellationToken))
        {
            throw new RbacException("权限编码已存在");
        }

        if (input.ParentId.HasValue)
        {
            await _permissionRepository.GetAsync(input.ParentId.Value, cancellationToken);
        }

        var permission = new Permission
        {
            ParentId = input.ParentId,
            Code = input.Code.Trim(),
            Name = input.Name.Trim(),
            Type = input.Type,
            Path = input.Path,
            Method = input.Method,
            Sort = input.Sort,
            IsEnabled = input.IsEnabled
        };

        await _permissionRepository.InsertAsync(permission, cancellationToken: cancellationToken);
        return RbacMapper.ToPermissionDto(permission);
    }

    public async Task<PermissionDto> UpdateAsync(long id, UpdatePermissionDto input, CancellationToken cancellationToken = default)
    {
        var permission = await _permissionRepository.GetAsync(id, cancellationToken);

        if (input.ParentId.HasValue)
        {
            if (input.ParentId.Value == id)
            {
                throw new RbacException("父级权限不能是自己");
            }

            await _permissionRepository.GetAsync(input.ParentId.Value, cancellationToken);
            permission.ParentId = input.ParentId;
        }

        if (input.Name != null) permission.Name = input.Name;
        if (input.Type.HasValue) permission.Type = input.Type.Value;
        if (input.Path != null) permission.Path = input.Path;
        if (input.Method != null) permission.Method = input.Method;
        if (input.Sort.HasValue) permission.Sort = input.Sort.Value;
        if (input.IsEnabled.HasValue) permission.IsEnabled = input.IsEnabled.Value;

        await _permissionRepository.UpdateAsync(permission, cancellationToken: cancellationToken);
        return RbacMapper.ToPermissionDto(permission);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var hasChildren = await _permissionRepository.AnyAsync(x => x.ParentId == id, cancellationToken);
        if (hasChildren)
        {
            throw new RbacException("请先删除子权限");
        }

        await _permissionRepository.DeleteAsync(id, cancellationToken: cancellationToken);
    }
}
