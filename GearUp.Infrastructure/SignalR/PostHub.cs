using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GearUp.Infrastructure.SignalR
{
    [Authorize]
    public class PostHub : Hub
    {
        public async Task JoinGroup(Guid postId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"post-{postId}");
        }

        public async Task JoinCommentsGroup(Guid commentId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"comment-{commentId}-replies");
        }

        public async Task LeaveGroup(Guid postId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"post-{postId}");
        }
    }
}
