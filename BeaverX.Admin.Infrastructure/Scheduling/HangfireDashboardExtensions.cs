using System.Reflection;
using BeaverX.Admin.Infrastructure.Dashboard;
using Hangfire.Dashboard;

namespace BeaverX.Admin.Infrastructure.Scheduling;

public static class HangfireDashboardExtensions
{
    private static int _registered;

    public static void RegisterCustomDashboardExtensions()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1)
        {
            return;
        }

        DashboardRoutes.Routes.Add("/recurring/cron/update", new RecurringCronUpdateDispatcher());

        var assembly = Assembly.GetExecutingAssembly();
        DashboardRoutes.AddJavaScript(assembly, "BeaverX.Admin.Infrastructure.Dashboard.recurring-cron-edit.js");
    }
}
