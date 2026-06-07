using System.Text;
using BeaverX.Admin.Application;
using BeaverX.Admin.Application.Dict;
using BeaverX.Admin.Application.Messages;
using BeaverX.Admin.Application.Rbac;
using BeaverX.Admin.EntityFrameworkCore;
using BeaverX.Admin.Http.Api;
using BeaverX.Admin.Http.Api.Authorization;
using BeaverX.Admin.Http.Api.Filters;
using BeaverX.Core.Modules;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = (WebApplication)context.App;

        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<RbacDataSeeder>();
        seeder.SeedAsync().GetAwaiter().GetResult();
        var messageSeeder = scope.ServiceProvider.GetRequiredService<MessageDataSeeder>();
        messageSeeder.SeedAsync().GetAwaiter().GetResult();
        var dictMenuSeeder = scope.ServiceProvider.GetRequiredService<DictMenuSeeder>();
        dictMenuSeeder.SeedAsync().GetAwaiter().GetResult();
        var dictDataSeeder = scope.ServiceProvider.GetRequiredService<DictDataSeeder>();
        dictDataSeeder.SeedAsync().GetAwaiter().GetResult();
    }
}
