using GearUp.Application.ServiceDtos;
using GearUp.Application.ServiceDtos.Message;
using GearUp.Application.ServiceDtos.Post;

namespace GearUp.Application.Interfaces
{
    public interface IRealTimeNotifier
    {
        Task BroadCastComments(Guid postId, CommentDto comment);
        Task BroadCastCommentLikes(Guid postId, Guid commentId, int likeCount);
        Task BroadCastPostLikes(Guid postId, int likeCount);
        Task PushNotification(Guid receiverId, NotificationDto notification);
        Task SendMessageToUser(Guid receiverId, MessageResponseDto message);
    }
}
