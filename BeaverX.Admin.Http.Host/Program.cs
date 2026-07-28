using BeaverX.Admin.Http.Host;
using Serilog;
using Volo.Abp;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting BeaverX Admin host");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseAutofac();

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    await builder.AddApplicationAsync<BeaverXAdminHttpHostModule>();

    var app = builder.Build();

    await app.InitializeApplicationAsync();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "BeaverX Admin host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
