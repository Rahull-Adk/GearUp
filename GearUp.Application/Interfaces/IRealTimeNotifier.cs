using GearUp.Application.ServiceDtos;
using GearUp.Application.ServiceDtos.Message;
using GearUp.Application.ServiceDtos.Post;

namespace GearUp.Application.Interfaces
{
    public interface IRealTimeNotifier
    {
        // Post-related
        Task BroadCastComments(Guid postId, CommentDto comment);
        Task BroadCastCommentLikes(Guid postId, Guid commentId, int likeCount);
        Task BroadCastPostLikes(Guid postId, int likeCount);

        // Notifications
        Task PushNotification(Guid receiverId, NotificationDto notification);

        // Messaging - User-based (for offline users / push notifications)
        Task SendMessageToUser(Guid receiverId, MessageResponseDto message);

        // Messaging - Conversation-based (for users in conversation)
        Task SendMessageToConversation(Guid conversationId, Guid excludeUserId, MessageResponseDto message);
        Task NotifyMessageEdited(Guid conversationId, Guid messageId, string newText, DateTime editedAt);
        Task NotifyMessageDeleted(Guid conversationId, Guid messageId);
    }
}
