using BeaverX.Admin.Domain.Shared;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace BeaverX.Admin.Domain;

[DependsOn(
    typeof(BeaverXAdminDomainSharedModule),
    typeof(AbpDddDomainModule)
)]
public class BeaverXAdminDomainModule : AbpModule
{
}
