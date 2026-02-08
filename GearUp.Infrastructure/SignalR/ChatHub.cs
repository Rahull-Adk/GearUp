using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GearUp.Infrastructure.SignalR
{
    [Authorize]
    public class ChatHub : Hub
    {
        public async Task JoinConversation(Guid conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");
        }

        public async Task LeaveConversation(Guid conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");
        }


        public async Task MarkMessagesAsRead(Guid conversationId, Guid lastReadMessageId)
        {
            var userId = Context.User?.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId)) return;

            await Clients.OthersInGroup($"conversation-{conversationId}")
                .SendAsync("MessagesRead", new { conversationId, userId, lastReadMessageId });
        }
    }
}

