namespace BeaverX.Admin.Application.Contracts.Rbac;

public interface IJwtTokenService
{
    (string Token, int ExpiresIn) CreateToken(
        long userId,
        string userName,
        IEnumerable<string> roles);
}
