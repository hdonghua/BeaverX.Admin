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
