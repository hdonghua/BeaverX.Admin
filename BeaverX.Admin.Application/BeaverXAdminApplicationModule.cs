using BeaverX.Admin.Application.Contracts;
using BeaverX.Admin.Application.Demo;
using BeaverX.Admin.Application.Payment;
using BeaverX.Admin.Domain;
using BeaverX.Admin.Domain.Shared.Demo;
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
            var services = context.Services;
            var configuration = context.Configuration;

            services.Configure<DemoModeOptions>(configuration.GetSection(DemoModeOptions.SectionName));
            services.AddHostedService<DemoModeStartupHostedService>();

            services.AddScoped<PaymentOrderAppService>();
            services.AddScoped<PaymentNotifyUrlBuilder>();
        }
    }
}
