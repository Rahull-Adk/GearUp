using GearUp.Application.Interfaces;
using GearUp.Application.ServiceDtos.Post;
using Microsoft.AspNetCore.SignalR;

namespace GearUp.Infrastructure.SignalR
{
    public  class SignalRRealTimeNotifier : IRealTimeNotifier
    {
        private readonly IHubContext<PostHub> _postHub;

        public SignalRRealTimeNotifier(IHubContext<PostHub> postHub)
        {
            _postHub = postHub;
        }

        public Task BroadCastComments(Guid postId, CommentDto comment)
            => _postHub.Clients
                .Group($"post-{postId}-comments")
                .SendAsync("CommentCreated", comment);

        public Task BroadCastCommentLikes(Guid postId, Guid commentId, int likeCount)
            => _postHub.Clients
                .Group($"post-{postId}-comments")
                .SendAsync("CommentLikeUpdated", new { commentId, likeCount });

        public Task BroadCastPostLikes(Guid postId, int likeCount)
            => _postHub.Clients
                .Group($"post-{postId}")
                .SendAsync("PostLikeUpdated", new { postId, likeCount });
    }
}