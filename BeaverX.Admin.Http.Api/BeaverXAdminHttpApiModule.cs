using BeaverX.Admin.Application.Contracts;
using BeaverX.Core.Modules;
using BeaverX.WebMvc;

namespace BeaverX.Admin.Http.Api;

[DependsOn(
    typeof(BeaverXWebMvcModule)
    )]
public class BeaverXAdminHttpApiModule : BeaverXModule
{
}
