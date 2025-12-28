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

        public Task BroadCastCommentToPostViewers(Guid postId, CommentDto comment)
            => _postHub.Clients
                .Group($"post-{postId}-comments")
                .SendAsync("CommentCreated", comment);

        public Task BroadCastCommentLikeUpdated(Guid postId, Guid commentId, int likeCount)
            => _postHub.Clients
                .Group($"post-{postId}-comments")
                .SendAsync("CommentLikeUpdated", new { commentId, likeCount });

        public Task BroadCastReplyAdded(Guid parentCommentId, CommentDto reply)
            => _postHub.Clients
                .Group($"comment-{parentCommentId}-replies")
                .SendAsync("ReplyAdded", reply);

        public Task BroadCastReplyLikeUpdated(Guid parentCommentId, Guid replyId, int likeCount)
            => _postHub.Clients
                .Group($"comment-{parentCommentId}-replies")
                .SendAsync("CommentLikeUpdated", new { commentId = replyId, likeCount });

        public Task BroadCastPostLikeUpdated(Guid postId, int likeCount)
            => _postHub.Clients
                .Group($"post-{postId}")
                .SendAsync("PostLikeUpdated", new { postId, likeCount });
    }
}