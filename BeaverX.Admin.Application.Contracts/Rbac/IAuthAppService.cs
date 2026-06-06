using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Rbac;

public interface IAuthAppService
{
    Task<LoginResultDto> LoginAsync(LoginDto input, CancellationToken cancellationToken = default);
    Task<UserProfileDto> GetProfileAsync(CancellationToken cancellationToken = default);
    Task<List<MenuDto>> GetCurrentUserMenusAsync(CancellationToken cancellationToken = default);
}
