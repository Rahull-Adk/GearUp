using GearUp.Application.Common.Pagination;
using GearUp.Application.ServiceDtos;
using GearUp.Domain.Entities.RealTime;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task AddNotificationAsync(Notification notification);
        Task<Notification?> GetNotificationByIdAsync(Guid notificationId);
        Task<CursorPageResult<NotificationDto>> GetNotificationsByUserIdAsync(Guid userId, Cursor? cursor, int pageSize = 20);
        Task<int> GetUnreadNotificationCountAsync(Guid userId);
        Task MarkNotificationAsReadAsync(Guid notificationId);
        Task MarkAllNotificationsAsReadAsync(Guid userId);
        Task DeleteNotificationAsync(Guid notificationId);
        Task DeleteAllNotificationsByUserIdAsync(Guid userId);
    }
}
