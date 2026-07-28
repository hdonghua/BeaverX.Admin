using BeaverX.Admin.Domain.Shared;
using Volo.Abp.Application;
using Volo.Abp.Modularity;

namespace BeaverX.Admin.Application.Contracts;

[DependsOn(
    typeof(BeaverXAdminDomainSharedModule),
    typeof(AbpDddApplicationContractsModule)
)]
public class BeaverXAdminApplicationContractModule : AbpModule
{
}
