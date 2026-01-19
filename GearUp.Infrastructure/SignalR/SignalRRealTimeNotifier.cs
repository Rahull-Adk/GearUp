using GearUp.Application.Interfaces;
using GearUp.Application.ServiceDtos;
using GearUp.Application.ServiceDtos.Post;
using Microsoft.AspNetCore.SignalR;

namespace GearUp.Infrastructure.SignalR
{
    public class SignalRRealTimeNotifier : IRealTimeNotifier
    {
        private readonly IHubContext<PostHub> _postHub;
        private readonly IHubContext<NotificationHub> _notificationHub;

        public SignalRRealTimeNotifier(IHubContext<PostHub> postHub, IHubContext<NotificationHub> notificationHub)
        {
            _postHub = postHub;
            _notificationHub = notificationHub;
        }

        public Task BroadCastComments(Guid postId, CommentDto comment) =>
            _postHub.Clients
                .Group($"post-{postId}-comments")
                .SendAsync("CommentCreated", comment);

        public Task BroadCastCommentLikes(Guid postId, Guid commentId, int likeCount) =>
            _postHub.Clients
                .Group($"post-{postId}-comments")
                .SendAsync("CommentLikeUpdated", new { commentId, likeCount });

        public Task BroadCastPostLikes(Guid postId, int likeCount) =>
            _postHub.Clients
                .Group($"post-{postId}")
                .SendAsync("PostLikeUpdated", new { postId, likeCount });

        public Task PushNotification(Guid receiverId, NotificationDto notification) =>
            _notificationHub.Clients
                .User(receiverId.ToString())
                .SendAsync("NotificationCreated", notification);

    }
}