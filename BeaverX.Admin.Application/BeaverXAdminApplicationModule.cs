using BeaverX.Admin.Application.Contracts;
using BeaverX.Admin.Application.Payment;
using BeaverX.Admin.Domain;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Application;
using Volo.Abp.Modularity;

namespace BeaverX.Admin.Application;

[DependsOn(
    typeof(BeaverXAdminApplicationContractModule),
    typeof(BeaverXAdminDomainModule),
    typeof(AbpDddApplicationModule)
)]
public class BeaverXAdminApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        services.AddScoped<PaymentOrderAppService>();
        services.AddScoped<PaymentNotifyUrlBuilder>();
    }
}
