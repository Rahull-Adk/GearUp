using GearUp.Application.Interfaces;
using GearUp.Application.ServiceDtos;
using GearUp.Application.ServiceDtos.Message;
using GearUp.Application.ServiceDtos.Post;
using Microsoft.AspNetCore.SignalR;

namespace GearUp.Infrastructure.SignalR
{
    public class SignalRRealTimeNotifier : IRealTimeNotifier
    {
        private readonly IHubContext<PostHub> _postHub;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly IHubContext<ChatHub> _chatHub;

        public SignalRRealTimeNotifier(
            IHubContext<PostHub> postHub,
            IHubContext<NotificationHub> notificationHub,
            IHubContext<ChatHub> chatHub)
        {
            _postHub = postHub;
            _notificationHub = notificationHub;
            _chatHub = chatHub;
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

        public Task SendMessageToUser(Guid receiverId, MessageResponseDto message) =>
            _chatHub.Clients
                .User(receiverId.ToString())
                .SendAsync("MessageReceived", message);

        public Task SendMessageToConversation(Guid conversationId, Guid excludeUserId, MessageResponseDto message) =>
            _chatHub.Clients
                .GroupExcept($"conversation-{conversationId}", GetConnectionIdsForUser(excludeUserId))
                .SendAsync("MessageReceived", message);

        public Task NotifyMessageEdited(Guid conversationId, Guid messageId, string newText, DateTime editedAt) =>
            _chatHub.Clients
                .Group($"conversation-{conversationId}")
                .SendAsync("MessageEdited", new { messageId, newText, editedAt });

        public Task NotifyMessageDeleted(Guid conversationId, Guid messageId) =>
            _chatHub.Clients
                .Group($"conversation-{conversationId}")
                .SendAsync("MessageDeleted", new { messageId });

        private static IReadOnlyList<string> GetConnectionIdsForUser(Guid _)
        {
            // Note: In a real implementation, you might want to track connection IDs per user
            // For now, we use empty list as GroupExcept with user ID isn't directly supported
            return Array.Empty<string>();
        }
    }
}