using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.Interfaces;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.ServiceDtos;
using GearUp.Domain.Entities.RealTime;
using GearUp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GearUp.Application.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ICommonRepository _commonRepository;
        private readonly IRealTimeNotifier _realTimeNotifier;
        private readonly ILogger<NotificationService> _logger;
        private readonly ICacheService _cacheService;
        private static readonly TimeSpan CountTtl = TimeSpan.FromSeconds(30);

        public NotificationService(
            INotificationRepository notificationRepository,
            ICommonRepository commonRepository,
            IRealTimeNotifier realTimeNotifier,
            ILogger<NotificationService> logger,
            ICacheService cacheService)
        {
            _notificationRepository = notificationRepository;
            _commonRepository = commonRepository;
            _realTimeNotifier = realTimeNotifier;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<NotificationDto> CreateAndPushNotificationAsync(
            string title,
            string content,
            NotificationEnum notificationType,
            Guid actorUserId,
            Guid receiverUserId,
            Guid? postId = null,
            Guid? commentId = null,
            Guid? appointmentId = null,
            Guid? kycId = null,
            Guid? carId = null)
        {
            _logger.LogInformation(
                "Creating notification for user {ReceiverId} from {ActorId}, type: {Type}",
                receiverUserId, actorUserId, notificationType);

            // Create the notification entity
            var notification = Notification.CreateNotification(
                title,
                content,
                notificationType,
                actorUserId,
                receiverUserId,
                postId,
                commentId,
                appointmentId,
                kycId,
                carId
            );

            // Persist to database
            await _notificationRepository.AddNotificationAsync(notification);
            await _commonRepository.SaveChangesAsync();
            await InvalidateUnreadCountCacheAsync(receiverUserId);

            // Create DTO for real-time push
            var notificationDto = new NotificationDto
            {
                Id = notification.Id,
                Title = notification.Title,
                Content = notification.Content,
                NotificationType = notification.NotificationType,
                ActorUserId = notification.ActorUserId,
                ReceiverUserId = notification.ReceiverUserId,
                PostId = notification.PostId,
                CommentId = notification.CommentId,
                AppointmentId = notification.AppointmentId,
                KycId = notification.KycId,
                CarId = notification.CarId,
                IsRead = false,
                SentAt = notification.CreatedAt
            };

            // Push real-time notification
            await _realTimeNotifier.PushNotification(receiverUserId, notificationDto);

            _logger.LogInformation(
                "Notification {NotificationId} created and pushed to user {ReceiverId}",
                notification.Id, receiverUserId);

            return notificationDto;
        }

        public async Task<Result<CursorPageResult<NotificationDto>>> GetNotificationsAsync(Guid userId, string? cursorString, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting notifications for user {UserId}", userId);

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    return Result<CursorPageResult<NotificationDto>>.Failure("Invalid cursor", 400);
                }
            }

            var notifications = await _notificationRepository.GetNotificationsByUserIdAsync(userId, cursor, pageSize, cancellationToken);

            return Result<CursorPageResult<NotificationDto>>.Success(notifications, "Notifications retrieved successfully");
        }

        public async Task<Result<int>> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting unread notification count for user {UserId}", userId);

            var cacheKey = GetUnreadCountCacheKey(userId);
            var cachedCount = await _cacheService.GetAsync<int?>(cacheKey);
            if (cachedCount.HasValue)
            {
                return Result<int>.Success(cachedCount.Value, "Unread count retrieved successfully");
            }

            var count = await _notificationRepository.GetUnreadNotificationCountAsync(userId, cancellationToken);
            await _cacheService.SetAsync(cacheKey, count, CountTtl);

            return Result<int>.Success(count, "Unread count retrieved successfully");
        }

        public async Task<Result<bool>> MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            _logger.LogInformation("Marking notification {NotificationId} as read for user {UserId}", notificationId, userId);

            var notification = await _notificationRepository.GetNotificationByIdAsync(notificationId);

            if (notification == null)
            {
                return Result<bool>.Failure("Notification not found", 404);
            }

            if (notification.ReceiverUserId != userId)
            {
                return Result<bool>.Failure("You cannot mark this notification as read", 403);
            }

            await _notificationRepository.MarkNotificationAsReadAsync(notificationId);
            await _commonRepository.SaveChangesAsync();
            await InvalidateUnreadCountCacheAsync(userId);

            return Result<bool>.Success(true, "Notification marked as read");
        }

        public async Task<Result<bool>> MarkAllAsReadAsync(Guid userId)
        {
            _logger.LogInformation("Marking all notifications as read for user {UserId}", userId);

            await _notificationRepository.MarkAllNotificationsAsReadAsync(userId);
            await _commonRepository.SaveChangesAsync();
            await InvalidateUnreadCountCacheAsync(userId);

            return Result<bool>.Success(true, "All notifications marked as read");
        }

        public async Task<Result<bool>> DeleteNotificationAsync(Guid notificationId, Guid userId)
        {
            _logger.LogInformation("Deleting notification {NotificationId} for user {UserId}", notificationId, userId);

            var notification = await _notificationRepository.GetNotificationByIdAsync(notificationId);

            if (notification == null)
            {
                return Result<bool>.Failure("Notification not found", 404);
            }

            if (notification.ReceiverUserId != userId)
            {
                return Result<bool>.Failure("You cannot delete this notification", 403);
            }

            await _notificationRepository.DeleteNotificationAsync(notificationId);
            await _commonRepository.SaveChangesAsync();
            await InvalidateUnreadCountCacheAsync(userId);

            return Result<bool>.Success(true, "Notification deleted");
        }

        public async Task<Result<bool>> DeleteAllNotificationsAsync(Guid userId)
        {
            _logger.LogInformation("Deleting all notifications for user {UserId}", userId);

            await _notificationRepository.DeleteAllNotificationsByUserIdAsync(userId);
            await _commonRepository.SaveChangesAsync();
            await InvalidateUnreadCountCacheAsync(userId);

            return Result<bool>.Success(true, "All notifications deleted");
        }

        private static string GetUnreadCountCacheKey(Guid userId) => $"notifications:unread-count:u:{userId}";
        private async Task InvalidateUnreadCountCacheAsync(Guid userId) => await _cacheService.RemoveAsync(GetUnreadCountCacheKey(userId));
    }
}
