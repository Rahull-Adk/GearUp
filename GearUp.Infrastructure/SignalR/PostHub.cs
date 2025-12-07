using Microsoft.AspNetCore.SignalR;

namespace GearUp.Infrastructure.SignalR
{
    public class PostHub : Hub
    {
        public async Task JoinGroup(string postId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"post-{postId}");
        }

        public async Task LeaveGroup(string postId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"post-{postId}");
        }
    }
}
