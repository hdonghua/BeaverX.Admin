using System.Text;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Http.Api;
using BeaverX.Admin.Http.Api.Authorization;
using BeaverX.Admin.Http.Api.Filters;
using BeaverX.Admin.Infrastructure;
using BeaverX.Admin.Infrastructure.Realtime;
using BeaverX.Core.Modules;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace BeaverX.Admin.Http.Host;

[DependsOn(
    typeof(BeaverXAdminInfrastructureModule),
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
            options.Filters.Add<BusinessExceptionFilter>();
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

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
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
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = (WebApplication)context.App;

        app.UseSerilogRequestLogging();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseBeaverXHangfire(app.Configuration);
        app.MapControllers();
        app.MapHub<AdminNotificationHub>("/hubs/notifications");
    }
}
