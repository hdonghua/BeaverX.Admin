using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.WebMvc.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeaverX.Admin.Http.Api.Controllers;

public class AuthController : BeaverXController
{
    private readonly IAuthAppService _authAppService;

    public AuthController(IAuthAppService authAppService)
    {
        _authAppService = authAppService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public Task<LoginResultDto> LoginAsync([FromBody] LoginDto input, CancellationToken cancellationToken)
        => _authAppService.LoginAsync(input, cancellationToken);

    [Authorize]
    [HttpGet("profile")]
    public Task<UserProfileDto> GetProfileAsync(CancellationToken cancellationToken)
        => _authAppService.GetProfileAsync(cancellationToken);

    [Authorize]
    [HttpPut("profile")]
    public Task<UserProfileDto> UpdateProfileAsync(
        [FromBody] UpdateProfileDto input,
        CancellationToken cancellationToken)
        => _authAppService.UpdateProfileAsync(input, cancellationToken);

    [Authorize]
    [HttpPut("password")]
    public Task ChangePasswordAsync(
        [FromBody] ChangePasswordDto input,
        CancellationToken cancellationToken)
        => _authAppService.ChangePasswordAsync(input, cancellationToken);

    [Authorize]
    [HttpGet("menus")]
    public Task<List<MenuDto>> GetMenusAsync(CancellationToken cancellationToken)
        => _authAppService.GetCurrentUserMenusAsync(cancellationToken);
}
