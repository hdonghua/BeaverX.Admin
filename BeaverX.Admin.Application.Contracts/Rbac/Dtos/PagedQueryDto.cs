namespace BeaverX.Admin.Application.Contracts.Rbac.Dtos;

public class PagedQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
