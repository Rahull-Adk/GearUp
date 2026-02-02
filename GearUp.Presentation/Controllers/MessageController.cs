using GearUp.Application.Interfaces.Services.MessageServiceInterface;
using GearUp.Application.ServiceDtos.Message;
using GearUp.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GearUp.Presentation.Controllers
{
    [Route("api/v1/messages")]
    [ApiController]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessageController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        /// <summary>
        /// Send a message to a user (customer to dealer or dealer to customer)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequestDto dto)
        {
            var userId = Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value);
            var result = await _messageService.SendMessageAsync(dto, userId);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        /// <summary>
        /// Get all conversations for the current user
        /// </summary>
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations([FromQuery] string? cursor)
        {
            var userId = Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value);
            var result = await _messageService.GetConversationsAsync(userId, cursor);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        /// <summary>
        /// Get a specific conversation with messages
        /// </summary>
        [HttpGet("conversations/{conversationId:guid}")]
        public async Task<IActionResult> GetConversation(
            [FromRoute] Guid conversationId,
            [FromQuery] string? cursor)
        {
            var userId = Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value);
            var result = await _messageService.GetConversationAsync(conversationId, userId, cursor);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        /// <summary>
        /// Get or create a conversation with another user
        /// </summary>
        [HttpGet("conversations/with/{otherUserId:guid}")]
        public async Task<IActionResult> GetOrCreateConversation([FromRoute] Guid otherUserId)
        {
            var userId = Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value);
            var result = await _messageService.GetOrCreateConversationWithUserAsync(userId, otherUserId);
            return StatusCode(result.Status, result.ToApiResponse());
        }

        /// <summary>
        /// Mark a conversation as read
        /// </summary>
        [HttpPost("conversations/{conversationId:guid}/read")]
        public async Task<IActionResult> MarkConversationAsRead([FromRoute] Guid conversationId)
        {
            var userId = Guid.Parse(User.FindFirst(u => u.Type == "id")!.Value);
            var result = await _messageService.MarkConversationAsReadAsync(conversationId, userId);
            return StatusCode(result.Status, result.ToApiResponse());
        }
    }
}
