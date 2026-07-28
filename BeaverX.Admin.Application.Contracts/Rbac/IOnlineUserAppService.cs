using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Contracts.Realtime;

namespace BeaverX.Admin.Application.Contracts.Rbac;

public interface IOnlineUserAppService
{
    Task<List<OnlineUserDto>> GetListAsync(CancellationToken cancellationToken = default);

    Task KickAsync(Guid userId, CancellationToken cancellationToken = default);
}
