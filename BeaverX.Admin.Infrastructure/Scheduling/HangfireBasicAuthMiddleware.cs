using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BeaverX.Admin.Infrastructure.Scheduling;

/// <summary>
/// 对 Hangfire Dashboard 启用 HTTP Basic 认证，浏览器打开时会自动弹出账号密码框。
/// </summary>
public class HangfireBasicAuthMiddleware
{
    private const string BasicScheme = "Basic ";
    private const string Realm = "BeaverX Hangfire";

    private readonly RequestDelegate _next;

    public HangfireBasicAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<HangfireOptions> hangfireOptions)
    {
        var options = hangfireOptions.Value;
        if (!options.EnableDashboard || !options.Auth.Enabled)
        {
            await _next(context);
            return;
        }

        if (!IsDashboardRequest(context, options.DashboardPath))
        {
            await _next(context);
            return;
        }

        if (TryValidateBasicCredentials(context, options.Auth))
        {
            await _next(context);
            return;
        }

        await ChallengeAsync(context);
    }

    private static bool IsDashboardRequest(HttpContext context, string dashboardPath)
    {
        if (string.IsNullOrWhiteSpace(dashboardPath))
        {
            dashboardPath = "/hangfire";
        }

        if (!dashboardPath.StartsWith('/'))
        {
            dashboardPath = "/" + dashboardPath;
        }

        return context.Request.Path.StartsWithSegments(dashboardPath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryValidateBasicCredentials(HttpContext context, HangfireDashboardAuthOptions auth)
    {
        if (string.IsNullOrWhiteSpace(auth.Username) || string.IsNullOrWhiteSpace(auth.Password))
        {
            return false;
        }

        if (!TryParseBasicHeader(context.Request.Headers.Authorization.ToString(), out var username, out var password))
        {
            return false;
        }

        var usernameMatch = FixedTimeEquals(username, auth.Username);
        var passwordMatch = FixedTimeEquals(password, auth.Password);
        return usernameMatch && passwordMatch;
    }

    private static bool TryParseBasicHeader(string? authorizationHeader, out string username, out string password)
    {
        username = string.Empty;
        password = string.Empty;

        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith(BasicScheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var encoded = authorizationHeader[BasicScheme.Length..].Trim();
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var separatorIndex = decoded.IndexOf(':');
            if (separatorIndex <= 0)
            {
                return false;
            }

            username = decoded[..separatorIndex];
            password = decoded[(separatorIndex + 1)..];
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool FixedTimeEquals(string actual, string expected)
    {
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }

    private static Task ChallengeAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = $"Basic realm=\"{Realm}\", charset=\"UTF-8\"";
        return Task.CompletedTask;
    }
}
