using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.ServiceDtos;

namespace GearUp.Application.Interfaces.Services
{
    public interface INotificationService
    {

        Task<NotificationDto> CreateAndPushNotificationAsync(
            string title,
            string content,
            Domain.Enums.NotificationEnum notificationType,
            Guid actorUserId,
            Guid receiverUserId,
            Guid? postId = null,
            Guid? commentId = null,
            Guid? appointmentId = null);


        Task<Result<CursorPageResult<NotificationDto>>> GetNotificationsAsync(Guid userId, string? cursor, int pageSize = 20);

        Task<Result<int>> GetUnreadCountAsync(Guid userId);

        Task<Result<bool>> MarkAsReadAsync(Guid notificationId, Guid userId);

        Task<Result<bool>> MarkAllAsReadAsync(Guid userId);

        Task<Result<bool>> DeleteNotificationAsync(Guid notificationId, Guid userId);

        Task<Result<bool>> DeleteAllNotificationsAsync(Guid userId);
    }
}

