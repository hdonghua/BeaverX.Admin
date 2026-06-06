using BeaverX.Admin.Application.Contracts;
using BeaverX.Core.Modules;
using BeaverX.Domain;

namespace BeaverX.Admin.Application
{
    [DependsOn(
        typeof(BeaverXAdminApplicationContractModule)
        )]
    public class BeaverXAdminApplicationModule : BeaverXModule
    {

    }
}
