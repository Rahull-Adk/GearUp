using GearUp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GearUp.Presentation.Controllers
{
    [Route("api/v1/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            userId = Guid.Empty;
            var rawId = User.FindFirst(u => u.Type == "id")?.Value;
            return !string.IsNullOrWhiteSpace(rawId) && Guid.TryParse(rawId, out userId);
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] string? cursor, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var result = await _notificationService.GetNotificationsAsync(userId, cursor, pageSize, cancellationToken);
            return StatusCode(result.Status, result);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var result = await _notificationService.GetUnreadCountAsync(userId, cancellationToken);
            return StatusCode(result.Status, result);
        }

        [HttpPatch("{notificationId:guid}/read")]
        public async Task<IActionResult> MarkAsRead([FromRoute] Guid notificationId)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var result = await _notificationService.MarkAsReadAsync(notificationId, userId);
            return StatusCode(result.Status, result);
        }

        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var result = await _notificationService.MarkAllAsReadAsync(userId);
            return StatusCode(result.Status, result);
        }

        [HttpDelete("{notificationId:guid}")]
        public async Task<IActionResult> DeleteNotification([FromRoute] Guid notificationId)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var result = await _notificationService.DeleteNotificationAsync(notificationId, userId);
            return StatusCode(result.Status, result);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAllNotifications()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var result = await _notificationService.DeleteAllNotificationsAsync(userId);
            return StatusCode(result.Status, result);
        }
    }
}
