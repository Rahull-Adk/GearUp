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

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] string? cursor, [FromQuery] int pageSize = 20)
        {
            var userId = User.FindFirst(u => u.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _notificationService.GetNotificationsAsync(Guid.Parse(userId), cursor, pageSize);
            return StatusCode(result.Status, result);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirst(u => u.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _notificationService.GetUnreadCountAsync(Guid.Parse(userId));
            return StatusCode(result.Status, result);
        }

        [HttpPatch("{notificationId:guid}/read")]
        public async Task<IActionResult> MarkAsRead([FromRoute] Guid notificationId)
        {
            var userId = User.FindFirst(u => u.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _notificationService.MarkAsReadAsync(notificationId, Guid.Parse(userId));
            return StatusCode(result.Status, result);
        }

        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirst(u => u.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _notificationService.MarkAllAsReadAsync(Guid.Parse(userId));
            return StatusCode(result.Status, result);
        }

        [HttpDelete("{notificationId:guid}")]
        public async Task<IActionResult> DeleteNotification([FromRoute] Guid notificationId)
        {
            var userId = User.FindFirst(u => u.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _notificationService.DeleteNotificationAsync(notificationId, Guid.Parse(userId));
            return StatusCode(result.Status, result);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAllNotifications()
        {
            var userId = User.FindFirst(u => u.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _notificationService.DeleteAllNotificationsAsync(Guid.Parse(userId));
            return StatusCode(result.Status, result);
        }
    }
}

