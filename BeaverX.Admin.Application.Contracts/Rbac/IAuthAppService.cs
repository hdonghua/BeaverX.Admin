using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Rbac;

public interface IAuthAppService
{
    Task<LoginResultDto> LoginAsync(LoginDto input, CancellationToken cancellationToken = default);
    Task<TokenResultDto> RefreshTokenAsync(RefreshTokenDto input, CancellationToken cancellationToken = default);
    Task LogoutAsync(RefreshTokenDto? input, CancellationToken cancellationToken = default);
    Task<UserProfileDto> GetProfileAsync(CancellationToken cancellationToken = default);
    Task<UserProfileDto> UpdateProfileAsync(UpdateProfileDto input, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(ChangePasswordDto input, CancellationToken cancellationToken = default);
    Task<List<MenuDto>> GetCurrentUserMenusAsync(CancellationToken cancellationToken = default);
}
