using BeaverX.Admin.Http.Host;
using BeaverX.Core;
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
