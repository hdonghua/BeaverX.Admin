using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Oa;

public class OaOrganizationOptionsDto
{
    public List<OaDepartmentOptionDto> Depts { get; set; } = [];
    public List<OaRoleOptionDto> Roles { get; set; } = [];
    public List<OaUserOptionDto> Users { get; set; } = [];
}

public class OaDepartmentOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid? ParentId { get; set; }
    public string? Code { get; set; }
    public List<OaDepartmentOptionDto> Children { get; set; } = [];
}

public class OaRoleOptionDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public class OaUserOptionDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? UserName { get; set; }
    public string? Avatar { get; set; }
}

public class OaDepartmentDetailsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Code { get; set; }
    public Guid? ParentId { get; set; }
    public string? ParentName { get; set; }
    public Guid? LeaderUserId { get; set; }
    public string? LeaderName { get; set; }
    public int MemberCount { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; }
}

public class OaDepartmentMemberDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Avatar { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsLeader { get; set; }
    public Guid? ManagerUserId { get; set; }
    public string? ManagerName { get; set; }
}

public class OaDepartmentMemberQuery : PagedQueryDto
{
    public string? Keyword { get; set; }
}

public class OaAddDepartmentMembersRequest
{
    public List<Guid> UserIds { get; set; } = [];
}

public class OaSetDepartmentLeaderRequest
{
    public Guid? LeaderUserId { get; set; }
}

public class OaSetMemberManagerRequest
{
    public Guid? ManagerUserId { get; set; }
}

public class OaSaveDepartmentRequest
{
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = null!;
    public string? Code { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;
}
