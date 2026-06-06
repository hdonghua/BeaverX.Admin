using BeaverX.Admin.Application;
using BeaverX.Admin.EntityFrameworkCore;
using BeaverX.Admin.Http.Api;
using BeaverX.Core.Modules;

namespace BeaverX.Admin.Http.Host
{
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

            services.AddControllers();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = (WebApplication)context.App;

            app.UseAuthorization();

            app.MapControllers();
        }
    }
}
