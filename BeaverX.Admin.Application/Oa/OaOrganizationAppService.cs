using BeaverX.Admin.Application.Contracts.Oa;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Domain.Oa;
using BeaverX.Admin.Domain.Rbac;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace BeaverX.Admin.Application.Oa;

public class OaOrganizationAppService : IOaOrganizationAppService, IScopedDependency
{
    private readonly IRepository<OaDepartment, Guid> _departmentRepository;
    private readonly IRepository<OaUserDepartment, Guid> _userDepartmentRepository;
    private readonly IRepository<Role, Guid> _roleRepository;
    private readonly IRepository<User, Guid> _userRepository;
    private readonly IGuidGenerator _ids;

    public OaOrganizationAppService(
        IRepository<OaDepartment, Guid> departmentRepository,
        IRepository<OaUserDepartment, Guid> userDepartmentRepository,
        IRepository<Role, Guid> roleRepository,
        IRepository<User, Guid> userRepository,
        IGuidGenerator ids)
    {
        _departmentRepository = departmentRepository;
        _userDepartmentRepository = userDepartmentRepository;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _ids = ids;
    }

    public async Task<List<OaDepartmentOptionDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        var departments = await (await _departmentRepository.GetQueryableAsync()).AsNoTracking()
            .Where(x => x.IsEnabled).OrderBy(x => x.Sort).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        return BuildTree(departments, null);
    }

    public async Task<OaDepartmentDetailsDto> GetDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        var department = await _departmentRepository.GetAsync(departmentId, cancellationToken: cancellationToken);
        var users = await _userRepository.GetQueryableAsync();
        var departments = await _departmentRepository.GetQueryableAsync();
        var leaderName = department.LeaderUserId.HasValue
            ? await users.Where(x => x.Id == department.LeaderUserId.Value).Select(x => x.NickName).FirstOrDefaultAsync(cancellationToken)
            : null;
        var parentName = department.ParentId.HasValue
            ? await departments.Where(x => x.Id == department.ParentId.Value).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var memberCount = await _userDepartmentRepository.CountAsync(x => x.DepartmentId == departmentId, cancellationToken);
        return new OaDepartmentDetailsDto
        {
            Id = department.Id,
            Name = department.Name,
            Code = department.Code,
            ParentId = department.ParentId,
            ParentName = parentName,
            LeaderUserId = department.LeaderUserId,
            LeaderName = leaderName,
            MemberCount = checked((int)memberCount),
            Sort = department.Sort,
            IsEnabled = department.IsEnabled
        };
    }

    public async Task<PagedResultDto<OaDepartmentMemberDto>> GetMembersAsync(Guid departmentId, OaDepartmentMemberQuery input, CancellationToken cancellationToken = default)
    {
        var department = await _departmentRepository.GetAsync(departmentId, cancellationToken: cancellationToken);
        var links = (await _userDepartmentRepository.GetQueryableAsync()).AsNoTracking().Where(x => x.DepartmentId == departmentId);
        var users = (await _userRepository.GetQueryableAsync()).AsNoTracking();
        var query = from link in links join user in users on link.UserId equals user.Id select new { link, user };
        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            query = query.Where(x => x.user.UserName.Contains(keyword) || x.user.NickName.Contains(keyword));
        }
        var total = await query.LongCountAsync(cancellationToken);
        var page = Math.Max(1, input.Page);
        var pageSize = Math.Clamp(input.PageSize, 1, 200);
        var items = await query.OrderByDescending(x => x.link.IsPrimary).ThenBy(x => x.user.UserName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new OaDepartmentMemberDto
            {
                UserId = x.user.Id,
                UserName = x.user.UserName,
                Name = x.user.NickName,
                Phone = x.user.Phone,
                Email = x.user.Email,
                Avatar = x.user.Avatar,
                IsPrimary = x.link.IsPrimary,
                IsLeader = department.LeaderUserId == x.user.Id,
                ManagerUserId = x.link.ManagerUserId
            }).ToListAsync(cancellationToken);
        var managerIds = items.Where(x => x.ManagerUserId.HasValue).Select(x => x.ManagerUserId!.Value).Distinct().ToList();
        var managerNames = managerIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await users.Where(x => managerIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.NickName ?? x.UserName, cancellationToken);
        foreach (var item in items)
            if (item.ManagerUserId.HasValue) item.ManagerName = managerNames.GetValueOrDefault(item.ManagerUserId.Value);
        return new PagedResultDto<OaDepartmentMemberDto> { Total = total, Items = items };
    }

    public async Task<List<OaUserOptionDto>> SearchUsersAsync(string? keyword, CancellationToken cancellationToken = default)
    {
        var query = (await _userRepository.GetQueryableAsync()).AsNoTracking().Where(x => x.IsEnabled);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x => x.UserName.Contains(value) || x.NickName.Contains(value) || (x.Phone != null && x.Phone.Contains(value)));
        }
        return await query.OrderBy(x => x.UserName).Take(30).Select(x => new OaUserOptionDto
        {
            Id = x.Id.ToString(),
            Name = x.NickName,
            UserName = x.UserName,
            Avatar = x.Avatar
        }).ToListAsync(cancellationToken);
    }

    public async Task<Guid> CreateDepartmentAsync(OaSaveDepartmentRequest input, CancellationToken cancellationToken = default)
    {
        var name = input.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessException("部门名称不能为空");
        if (input.ParentId.HasValue && !await _departmentRepository.AnyAsync(x => x.Id == input.ParentId.Value, cancellationToken))
            throw new BusinessException("上级部门不存在");
        var code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim();
        if (code != null && await _departmentRepository.AnyAsync(x => x.Code == code, cancellationToken))
            throw new BusinessException("部门编码已存在");
        var department = new OaDepartment(_ids.Create())
        {
            ParentId = input.ParentId,
            Name = name,
            Code = code,
            Sort = input.Sort,
            IsEnabled = input.IsEnabled
        };
        await _departmentRepository.InsertAsync(department, autoSave: true, cancellationToken: cancellationToken);
        return department.Id;
    }

    public async Task UpdateDepartmentAsync(Guid departmentId, OaSaveDepartmentRequest input, CancellationToken cancellationToken = default)
    {
        var department = await _departmentRepository.GetAsync(departmentId, cancellationToken: cancellationToken);
        var name = input.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessException("部门名称不能为空");
        if (input.ParentId == departmentId) throw new BusinessException("不能将部门自身设为上级部门");
        if (input.ParentId.HasValue)
        {
            var descendants = await GetDescendantIdsAsync(departmentId, cancellationToken);
            if (descendants.Contains(input.ParentId.Value)) throw new BusinessException("不能将下级部门设为上级部门");
            if (!await _departmentRepository.AnyAsync(x => x.Id == input.ParentId.Value, cancellationToken)) throw new BusinessException("上级部门不存在");
        }
        var code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim();
        if (code != null && await _departmentRepository.AnyAsync(x => x.Id != departmentId && x.Code == code, cancellationToken))
            throw new BusinessException("部门编码已存在");
        department.ParentId = input.ParentId;
        department.Name = name;
        department.Code = code;
        department.Sort = input.Sort;
        department.IsEnabled = input.IsEnabled;
        await _departmentRepository.UpdateAsync(department, autoSave: true, cancellationToken: cancellationToken);
    }

    public async Task DeleteDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        var department = await _departmentRepository.GetAsync(departmentId, cancellationToken: cancellationToken);
        if (await _departmentRepository.AnyAsync(x => x.ParentId == departmentId, cancellationToken)) throw new BusinessException("请先删除下级部门");
        if (await _userDepartmentRepository.AnyAsync(x => x.DepartmentId == departmentId, cancellationToken)) throw new BusinessException("部门存在成员，不能删除");
        await _departmentRepository.DeleteAsync(department, autoSave: true, cancellationToken: cancellationToken);
    }

    private async Task<HashSet<Guid>> GetDescendantIdsAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        var departments = await (await _departmentRepository.GetQueryableAsync()).AsNoTracking().Select(x => new { x.Id, x.ParentId }).ToListAsync(cancellationToken);
        var result = new HashSet<Guid>();
        var pending = new Queue<Guid>();
        pending.Enqueue(departmentId);
        while (pending.TryDequeue(out var parentId))
            foreach (var child in departments.Where(x => x.ParentId == parentId))
                if (result.Add(child.Id)) pending.Enqueue(child.Id);
        return result;
    }

    public async Task AddMembersAsync(Guid departmentId, OaAddDepartmentMembersRequest input, CancellationToken cancellationToken = default)
    {
        await _departmentRepository.GetAsync(departmentId, cancellationToken: cancellationToken);
        var userIds = input.UserIds.Where(x => x != Guid.Empty).Distinct().ToList();
        if (userIds.Count == 0) return;
        var validUserIds = await (await _userRepository.GetQueryableAsync()).AsNoTracking().Where(x => userIds.Contains(x.Id) && x.IsEnabled).Select(x => x.Id).ToListAsync(cancellationToken);
        if (validUserIds.Count != userIds.Count) throw new BusinessException("部分用户不存在或已停用");
        var existing = await (await _userDepartmentRepository.GetQueryableAsync()).AsNoTracking().Where(x => validUserIds.Contains(x.UserId)).ToListAsync(cancellationToken);
        var linked = existing.Where(x => x.DepartmentId == departmentId).Select(x => x.UserId).ToHashSet();
        var primaryUsers = existing.Where(x => x.IsPrimary).Select(x => x.UserId).ToHashSet();
        var additions = validUserIds.Where(x => !linked.Contains(x)).Select(userId => new OaUserDepartment(_ids.Create())
        {
            DepartmentId = departmentId,
            UserId = userId,
            IsPrimary = !primaryUsers.Contains(userId)
        }).ToList();
        if (additions.Count > 0) await _userDepartmentRepository.InsertManyAsync(additions, autoSave: true, cancellationToken: cancellationToken);
    }

    public async Task RemoveMemberAsync(Guid departmentId, Guid userId, CancellationToken cancellationToken = default)
    {
        var department = await _departmentRepository.GetAsync(departmentId, cancellationToken: cancellationToken);
        if (department.LeaderUserId == userId) throw new BusinessException("该员工是部门负责人，请先更换负责人");
        var link = await _userDepartmentRepository.FindAsync(x => x.DepartmentId == departmentId && x.UserId == userId, cancellationToken: cancellationToken);
        if (link != null) await _userDepartmentRepository.DeleteAsync(link, autoSave: true, cancellationToken: cancellationToken);
    }

    public async Task SetLeaderAsync(Guid departmentId, OaSetDepartmentLeaderRequest input, CancellationToken cancellationToken = default)
    {
        var department = await _departmentRepository.GetAsync(departmentId, cancellationToken: cancellationToken);
        if (!input.LeaderUserId.HasValue)
        {
            department.LeaderUserId = null;
            await _departmentRepository.UpdateAsync(department, autoSave: true, cancellationToken: cancellationToken);
            return;
        }
        var userId = input.LeaderUserId.Value;
        if (!await _userRepository.AnyAsync(x => x.Id == userId && x.IsEnabled, cancellationToken)) throw new BusinessException("用户不存在或已停用");
        if (await _departmentRepository.AnyAsync(x => x.Id != departmentId && x.LeaderUserId == userId, cancellationToken))
            throw new BusinessException("该员工已经是其他部门的主岗负责人");

        var links = await (await _userDepartmentRepository.GetQueryableAsync()).Where(x => x.UserId == userId).ToListAsync(cancellationToken);
        foreach (var link in links) link.IsPrimary = link.DepartmentId == departmentId;
        var current = links.FirstOrDefault(x => x.DepartmentId == departmentId);
        if (current == null)
            await _userDepartmentRepository.InsertAsync(new OaUserDepartment(_ids.Create()) { DepartmentId = departmentId, UserId = userId, IsPrimary = true }, autoSave: false, cancellationToken: cancellationToken);
        if (links.Count > 0) await _userDepartmentRepository.UpdateManyAsync(links, autoSave: false, cancellationToken: cancellationToken);
        department.LeaderUserId = userId;
        await _departmentRepository.UpdateAsync(department, autoSave: true, cancellationToken: cancellationToken);
    }

    public async Task SetMemberManagerAsync(Guid departmentId, Guid userId, OaSetMemberManagerRequest input, CancellationToken cancellationToken = default)
    {
        var membership = await _userDepartmentRepository.FindAsync(
            x => x.DepartmentId == departmentId && x.UserId == userId,
            cancellationToken: cancellationToken) ?? throw new BusinessException("部门成员不存在");
        if (!membership.IsPrimary) throw new BusinessException("只能为员工的主岗设置直属上级");
        if (input.ManagerUserId == userId) throw new BusinessException("不能将员工本人设置为直属上级");
        if (input.ManagerUserId.HasValue &&
            !await _userRepository.AnyAsync(x => x.Id == input.ManagerUserId.Value && x.IsEnabled, cancellationToken))
            throw new BusinessException("直属上级不存在或已停用");

        if (input.ManagerUserId.HasValue)
        {
            var primaryLinks = await (await _userDepartmentRepository.GetQueryableAsync()).AsNoTracking()
                .Where(x => x.IsPrimary).Select(x => new { x.UserId, x.ManagerUserId }).ToListAsync(cancellationToken);
            var managerMap = primaryLinks.ToDictionary(x => x.UserId, x => x.ManagerUserId);
            var current = input.ManagerUserId;
            var visited = new HashSet<Guid>();
            while (current.HasValue && visited.Add(current.Value))
            {
                if (current.Value == userId) throw new BusinessException("直属上级关系不能形成循环");
                current = managerMap.GetValueOrDefault(current.Value);
            }
        }

        membership.ManagerUserId = input.ManagerUserId;
        await _userDepartmentRepository.UpdateAsync(membership, autoSave: true, cancellationToken: cancellationToken);
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
                Name = x.NickName,
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
