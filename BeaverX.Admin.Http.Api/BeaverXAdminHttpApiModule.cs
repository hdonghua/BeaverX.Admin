using BeaverX.Admin.Application.Contracts;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Modularity;

namespace BeaverX.Admin.Http.Api;

[DependsOn(
    typeof(BeaverXAdminApplicationContractModule),
    typeof(AbpAspNetCoreMvcModule)
)]
public class BeaverXAdminHttpApiModule : AbpModule
{
}
