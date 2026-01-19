using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GearUp.Infrastructure.SignalR
{
    [Authorize ]
    public class NotificationHub : Hub
    {
    }
}