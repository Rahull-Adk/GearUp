using GearUp.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace GearUp.Infrastructure.SignalR
{
    public class SignalRRealTimeNotifier : IRealTimeNotifier
    {
        private readonly IHubContext<PostHub> _postHub;
        public SignalRRealTimeNotifier(IHubContext<PostHub> postHub)
        {
            _postHub = postHub;
        }
        public async Task BroadCastCommentToPostViewers(Guid postId)
        {
           await _postHub.Clients.Group($"post-{postId}").SendAsync("CommentAdded");
        }

        public async Task BroadCastCommentLikesToPostViewers(Guid postId)
        {
            await _postHub.Clients.Group($"post-{postId}").SendAsync("UpdatedCommentLike");
        }

        public async Task BroadCastLikesToPostViewers(Guid postId)
        {
            await _postHub.Clients.Group($"post-{postId}").SendAsync("UpdatedPostLike");
        }
    }
}
