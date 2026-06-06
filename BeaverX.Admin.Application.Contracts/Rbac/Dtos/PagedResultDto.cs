namespace BeaverX.Admin.Application.Contracts.Rbac.Dtos;

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = [];
    public long Total { get; set; }
}
