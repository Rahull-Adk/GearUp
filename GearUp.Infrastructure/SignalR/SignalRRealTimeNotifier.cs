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
        public async Task BroadCastCommentToPostViewers(Guid postId, string comment)
        {
           await _postHub.Clients.Group($"post-{postId}").SendAsync("ReceiveComment", comment);
        }

        public async Task BroadCastLikesToCommentViewers(Guid commentId, int likeCount)
        {
            await _postHub.Clients.Group($"comment-{commentId}").SendAsync("ReceiveCommentLike", likeCount);
        }

        public async Task BroadCastLikeToPostViewers(Guid postId, int likeCount)
        {
            await _postHub.Clients.Group($"post-{postId}").SendAsync("ReceiveLike", likeCount);
        }
    }
}
