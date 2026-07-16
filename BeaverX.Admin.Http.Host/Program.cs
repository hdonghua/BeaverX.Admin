using BeaverX.Admin.Http.Host;
using BeaverX.Admin.SqlSugar;
using BeaverX.Core;
using BeaverX.Data.SqlSugar.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using SqlSugar;

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

#if DEBUG
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
    var options = scope.ServiceProvider.GetRequiredService<IOptions<BeaverXSqlSugarOptions>>();
    db.DbMaintenance.CreateDatabase();
    db.CodeFirst.InitTables(options.Value.EntityTypes.ToArray());
#endif 

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
