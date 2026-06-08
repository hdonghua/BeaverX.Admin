using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BeaverX.Admin.Infrastructure.Realtime;

[Authorize]
public class AdminNotificationHub : Hub
{
}
