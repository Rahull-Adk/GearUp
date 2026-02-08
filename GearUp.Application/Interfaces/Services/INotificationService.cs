using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.ServiceDtos;

namespace GearUp.Application.Interfaces.Services
{
    public interface INotificationService
    {
        /// <summary>
        /// Creates a notification, saves it to the database, and pushes it in real-time
        /// </summary>
        Task<NotificationDto> CreateAndPushNotificationAsync(
            string title,
            Domain.Enums.NotificationEnum notificationType,
            Guid actorUserId,
            Guid receiverUserId,
            Guid? postId = null,
            Guid? commentId = null,
            Guid? appointmentId = null);

        /// <summary>
        /// Get cursor-paginated notifications for a user
        /// </summary>
        Task<Result<CursorPageResult<NotificationDto>>> GetNotificationsAsync(Guid userId, string? cursor, int pageSize = 20);

        /// <summary>
        /// Get unread notification count for a user
        /// </summary>
        Task<Result<int>> GetUnreadCountAsync(Guid userId);

        /// <summary>
        /// Mark a single notification as read
        /// </summary>
        Task<Result<bool>> MarkAsReadAsync(Guid notificationId, Guid userId);

        /// <summary>
        /// Mark all notifications as read for a user
        /// </summary>
        Task<Result<bool>> MarkAllAsReadAsync(Guid userId);

        /// <summary>
        /// Delete a notification
        /// </summary>
        Task<Result<bool>> DeleteNotificationAsync(Guid notificationId, Guid userId);

        /// <summary>
        /// Delete all notifications for a user
        /// </summary>
        Task<Result<bool>> DeleteAllNotificationsAsync(Guid userId);
    }
}

