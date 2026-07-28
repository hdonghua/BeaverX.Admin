using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace BeaverX.Admin.Domain.Shared;

[DependsOn(typeof(AbpDddDomainSharedModule))]
public class BeaverXAdminDomainSharedModule : AbpModule
{
}
