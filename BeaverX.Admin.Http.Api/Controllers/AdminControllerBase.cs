using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class AdminControllerBase : AbpControllerBase
{
}
