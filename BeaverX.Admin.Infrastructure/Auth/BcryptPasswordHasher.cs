using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Core.Dependency;

namespace BeaverX.Admin.Infrastructure.Auth;

public class BcryptPasswordHasher : IPasswordHasher, IScopedDependency
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
