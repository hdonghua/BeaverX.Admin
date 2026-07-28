using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Rbac;

public interface IUserAppService
{
    Task<PagedResultDto<UserDto>> GetListAsync(UserQueryDto input, CancellationToken cancellationToken = default);
    Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDto> CreateAsync(CreateUserDto input, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserDto input, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task AssignRolesAsync(Guid id, AssignUserRolesDto input, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(Guid id, ResetPasswordDto input, CancellationToken cancellationToken = default);
}
