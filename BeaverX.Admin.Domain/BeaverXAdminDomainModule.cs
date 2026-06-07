using BeaverX.Admin.Domain.DataSeeder;
using BeaverX.Admin.Domain.Shared;
using BeaverX.Core.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace BeaverX.Admin.Domain
{
    [DependsOn(
        typeof(BeaverXAdminDomainSharedModule)
    )]
    public class BeaverXAdminDomainModule : BeaverXModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddHostedService<DataSeederHostService>();
        }
    }
}
