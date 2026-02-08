using GearUp.Application.Common.Pagination;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.ServiceDtos;
using GearUp.Domain.Entities.RealTime;
using GearUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GearUp.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly GearUpDbContext _db;

        public NotificationRepository(GearUpDbContext db)
        {
            _db = db;
        }

        public async Task AddNotificationAsync(Notification notification)
        {
            await _db.Notifications.AddAsync(notification);
        }

        public async Task<Notification?> GetNotificationByIdAsync(Guid notificationId)
        {
            return await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId);
        }

        public async Task<CursorPageResult<NotificationDto>> GetNotificationsByUserIdAsync(Guid userId, Cursor? cursor, int pageSize = 20)
        {
            IQueryable<Notification> query = _db.Notifications
                .AsNoTracking()
                .Where(n => n.ReceiverUserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id);

            if (cursor is not null)
            {
                query = query.Where(n => n.CreatedAt < cursor.CreatedAt ||
                    (n.CreatedAt == cursor.CreatedAt && n.Id.CompareTo(cursor.Id) < 0));
            }

            var rows = await query
                .Take(pageSize + 1)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    NotificationType = n.NotificationType,
                    ReceiverUserId = n.ReceiverUserId,
                    ActorUserId = n.ActorUserId,
                    IsRead = n.IsRead,
                    PostId = n.PostId,
                    CommentId = n.CommentId,
                    KycId = n.KycId,
                    AppointmentId = n.AppointmentId,
                    SentAt = n.CreatedAt
                })
                .ToListAsync();

            bool hasMore = rows.Count > pageSize;
            string? nextCursor = null;

            if (hasMore)
            {
                var lastItem = rows[pageSize - 1];
                nextCursor = Cursor.Encode(new Cursor
                {
                    CreatedAt = lastItem.SentAt,
                    Id = lastItem.Id
                });
            }

            return new CursorPageResult<NotificationDto>
            {
                Items = rows.Take(pageSize).ToList(),
                NextCursor = nextCursor,
                HasMore = hasMore
            };
        }

        public async Task<int> GetUnreadNotificationCountAsync(Guid userId)
        {
            return await _db.Notifications
                .AsNoTracking()
                .CountAsync(n => n.ReceiverUserId == userId && !n.IsRead);
        }

        public async Task MarkNotificationAsReadAsync(Guid notificationId)
        {
            var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId);
            notification?.MarkAsRead();
        }

        public async Task MarkAllNotificationsAsReadAsync(Guid userId)
        {
            var notifications = await _db.Notifications
                .Where(n => n.ReceiverUserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.MarkAsRead();
            }
        }

        public async Task DeleteNotificationAsync(Guid notificationId)
        {
            var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId);
            if (notification != null)
            {
                _db.Notifications.Remove(notification);
            }
        }

        public async Task DeleteAllNotificationsByUserIdAsync(Guid userId)
        {
            var notifications = await _db.Notifications
                .Where(n => n.ReceiverUserId == userId)
                .ToListAsync();

            _db.Notifications.RemoveRange(notifications);
        }
    }
}
