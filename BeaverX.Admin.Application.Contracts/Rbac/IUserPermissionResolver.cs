namespace BeaverX.Admin.Application.Contracts.Rbac;

public interface IUserPermissionResolver
{
    Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
