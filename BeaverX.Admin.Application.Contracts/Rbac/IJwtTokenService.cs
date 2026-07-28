namespace BeaverX.Admin.Application.Contracts.Rbac;

public interface IJwtTokenService
{
    (string Token, int ExpiresIn) CreateToken(
        Guid userId,
        string userName,
        IEnumerable<string> roles);
}
