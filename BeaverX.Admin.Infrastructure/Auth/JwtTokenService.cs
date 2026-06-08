using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Core.Dependency;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BeaverX.Admin.Infrastructure.Auth;

public class JwtTokenService : IJwtTokenService, IScopedDependency
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public (string Token, int ExpiresIn) CreateToken(
        long userId,
        string userName,
        IEnumerable<string> roles,
        IEnumerable<string> permissions)
    {
        var expiresIn = _options.ExpiresInMinutes * 60;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, userName),
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim(RbacClaimTypes.Permission, permission)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_options.ExpiresInMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresIn);
    }
}
