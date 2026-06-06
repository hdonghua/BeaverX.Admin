using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Rbac;

public interface IUserAppService
{
    Task<PagedResultDto<UserDto>> GetListAsync(UserQueryDto input, CancellationToken cancellationToken = default);
    Task<UserDto> GetAsync(long id, CancellationToken cancellationToken = default);
    Task<UserDto> CreateAsync(CreateUserDto input, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateAsync(long id, UpdateUserDto input, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task AssignRolesAsync(long id, AssignUserRolesDto input, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(long id, ResetPasswordDto input, CancellationToken cancellationToken = default);
}
