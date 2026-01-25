using GearUp.Application.ServiceDtos.Message;
using GearUp.Domain.Entities.RealTime;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface IMessageRepository
    {
        Task<Conversation?> GetConversationByIdAsync(Guid conversationId);
        Task<Conversation?> GetConversationByParticipantsAsync(Guid userId1, Guid userId2);
        Task<List<ConversationResponseDto>> GetUserConversationsAsync(Guid userId);
        Task<List<Message>> GetConversationMessagesAsync(Guid conversationId, int page = 1, int pageSize = 50);
        Task<bool> IsParticipantInConversationAsync(Guid conversationId, Guid userId);
        Task AddConversationAsync(Conversation conversation);
        Task AddMessageAsync(Message message);
        Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId);
        Task MarkMessagesAsReadAsync(Guid conversationId, Guid userId);
    }
}
