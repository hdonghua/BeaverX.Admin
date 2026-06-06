using BeaverX.Admin.Domain.Shared;
using BeaverX.Core.Modules;

namespace BeaverX.Admin.Domain
{
    [DependsOn(
        typeof(BeaverXAdminDomainSharedModule)
    )]
    public class BeaverXAdminDomainModule : BeaverXModule
    {
    }
}
