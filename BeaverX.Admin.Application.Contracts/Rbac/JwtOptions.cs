namespace BeaverX.Admin.Application.Contracts.Rbac;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "BeaverX.Admin";
    public string Audience { get; set; } = "BeaverX.Admin";
    public string SecretKey { get; set; } = null!;
    public int ExpiresInMinutes { get; set; } = 120;
    public int RefreshTokenExpiresInDays { get; set; } = 7;
}
