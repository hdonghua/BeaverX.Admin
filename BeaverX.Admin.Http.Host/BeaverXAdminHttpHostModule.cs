using System.Text;
using BeaverX.Admin.Application;
using BeaverX.Admin.Application.Rbac;
using BeaverX.Admin.EntityFrameworkCore;
using BeaverX.Admin.Http.Api;
using BeaverX.Admin.Http.Api.Authorization;
using BeaverX.Admin.Http.Api.Filters;
using BeaverX.Core.Modules;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Savorboard.CAP.InMemoryMessageQueue;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace BeaverX.Admin.Http.Host;

[DependsOn(
    typeof(BeaverXAdminEntityFrameworkCoreModule),
    typeof(BeaverXAdminApplicationModule),
    typeof(BeaverXAdminHttpApiModule)
)]
public class BeaverXAdminHttpHostModule : BeaverXModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        var configuration = context.Configuration;

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration is required.");

        services.AddControllers(options =>
        {
            options.Filters.Add<RbacExceptionFilter>();
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();
        services.AddRbacPermissionAuthorization();

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(configuration["CorsOrgins"]!.Split(','))
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is required.");

        services.AddCap(options =>
        {
            options.UsePostgreSql(postgresOptions =>
            {
                postgresOptions.ConnectionString = connectionString;
                postgresOptions.Schema = "cap";
            });
            options.UseInMemoryMessageQueue();
            options.FailedRetryCount = 5;
            options.FailedRetryInterval = 60;
            options.CollectorCleaningInterval = 3600;
            options.SucceedMessageExpiredAfter = 24 * 3600;
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = (WebApplication)context.App;

        app.UseSerilogRequestLogging();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }
}
