namespace BeaverX.Admin.Application.Contracts.Rbac;

public interface IUserPermissionResolver
{
    Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        long userId,
        CancellationToken cancellationToken = default);
}
