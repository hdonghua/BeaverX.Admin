using BeaverX.Admin.Application.Contracts.Oa;
using BeaverX.Admin.Domain.Oa;
using BeaverX.Admin.Domain.Rbac;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace BeaverX.Admin.Application.Oa;

public class OaOrganizationAppService : IOaOrganizationAppService, IScopedDependency
{
    private readonly IRepository<OaDepartment, Guid> _departmentRepository;
    private readonly IRepository<Role, Guid> _roleRepository;
    private readonly IRepository<User, Guid> _userRepository;

    public OaOrganizationAppService(
        IRepository<OaDepartment, Guid> departmentRepository,
        IRepository<Role, Guid> roleRepository,
        IRepository<User, Guid> userRepository)
    {
        _departmentRepository = departmentRepository;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
    }

    public async Task<OaOrganizationOptionsDto> GetOptionsAsync(CancellationToken cancellationToken = default)
    {
        var departments = await (await _departmentRepository.GetQueryableAsync())
            .AsNoTracking().Where(x => x.IsEnabled).OrderBy(x => x.Sort).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
        var roles = await (await _roleRepository.GetQueryableAsync())
            .AsNoTracking().Where(x => x.IsEnabled).OrderBy(x => x.Sort).ThenBy(x => x.Name)
            .Select(x => new OaRoleOptionDto { Id = x.Id.ToString(), Name = x.Name, Description = x.Description })
            .ToListAsync(cancellationToken);
        var users = await (await _userRepository.GetQueryableAsync())
            .AsNoTracking().Where(x => x.IsEnabled).OrderBy(x => x.UserName)
            .Select(x => new OaUserOptionDto
            {
                Id = x.Id.ToString(),
                Name = x.NickName ?? x.UserName,
                UserName = x.UserName,
                Avatar = x.Avatar
            }).ToListAsync(cancellationToken);

        return new OaOrganizationOptionsDto
        {
            Depts = BuildTree(departments, null),
            Roles = roles,
            Users = users
        };
    }

    private static List<OaDepartmentOptionDto> BuildTree(List<OaDepartment> source, Guid? parentId) =>
        source.Where(x => x.ParentId == parentId)
            .Select(x => new OaDepartmentOptionDto
            {
                Id = x.Id,
                Name = x.Name,
                ParentId = x.ParentId,
                Code = x.Code,
                Children = BuildTree(source, x.Id)
            }).ToList();
}
