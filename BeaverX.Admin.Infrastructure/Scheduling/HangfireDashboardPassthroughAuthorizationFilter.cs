using Hangfire.Dashboard;

namespace BeaverX.Admin.Infrastructure.Scheduling;

/// <summary>
/// Dashboard 访问已由 <see cref="HangfireBasicAuthMiddleware"/> 校验，此处直接放行。
/// </summary>
public sealed class HangfireDashboardPassthroughAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
