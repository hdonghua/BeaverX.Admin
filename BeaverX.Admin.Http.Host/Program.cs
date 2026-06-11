using BeaverX.Admin.Http.Host;
using BeaverX.Admin.Infrastructure.Scheduling;
using BeaverX.Core;
using Hangfire;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting BeaverX Admin host");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.AddBeaverX<BeaverXAdminHttpHostModule>();

    var app = builder.Build();

    app.InitializeBeaverX();

    // 勿在每次启动时 RecurringJob.AddOrUpdate(..., Cron.Daily)，会覆盖面板里改过的 Cron。
    // 仅首次注册请用 AddOrUpdateIfNotExists，或改到「定时任务」管理页维护。
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        HangfireRecurringJobStartup.AddOrUpdateIfNotExists(
            "myrecurringjob",
            () => Console.WriteLine("Recurring!"),
            Cron.Daily());
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "BeaverX Admin host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
