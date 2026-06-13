namespace BeaverX.Admin.Infrastructure.Scheduling;

/// <summary>
/// Hangfire Dashboard 独立账号密码（HTTP Basic），与业务 JWT 无关。
/// </summary>
public class HangfireDashboardAuthOptions
{
    public bool Enabled { get; set; } = true;

    public string Username { get; set; } = "hangfire";

    public string Password { get; set; } = string.Empty;
}
