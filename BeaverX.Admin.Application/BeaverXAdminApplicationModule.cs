using BeaverX.Admin.Application.Contracts;
using BeaverX.Admin.Domain;
using BeaverX.Core.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace BeaverX.Admin.Application
{
    [DependsOn(
        typeof(BeaverXAdminApplicationContractModule),
        typeof(BeaverXAdminDomainModule)
    )]
    public class BeaverXAdminApplicationModule : BeaverXModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}
