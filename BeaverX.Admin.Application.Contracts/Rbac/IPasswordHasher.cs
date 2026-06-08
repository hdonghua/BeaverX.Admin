namespace BeaverX.Admin.Application.Contracts.Rbac;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
