using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace BeaverX.Admin.Infrastructure.Realtime;

public class UserIdHubConnectionProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
