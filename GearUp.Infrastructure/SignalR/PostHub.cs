using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GearUp.Infrastructure.SignalR
{

    public class PostHub : Hub
    {
        public async Task JoinGroup(Guid postId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"post-{postId}");
        }

        public async Task JoinCommentsGroup(Guid postId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"post-{postId}-comments");
        }

        public async Task LeaveCommentsGroup(Guid postId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"post-{postId}-comments");
        }

        public async Task LeaveGroup(Guid postId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"post-{postId}");
        }
    }
}
