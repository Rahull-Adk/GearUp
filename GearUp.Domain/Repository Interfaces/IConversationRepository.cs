
using GearUp.Domain.Entities.Chats;

namespace GearUp.Domain.Repository_Interfaces
{
    public interface IConversationRepository
    {
        Task<Conversation?> GetConversationByIdAsync(Guid conversationId);
        Task<IEnumerable<Conversation>> GetAllConversationsAsync();
        Task<IEnumerable<Conversation>> GetConversationsByUserIdAsync(Guid userId);
        Task AddConversationAsync(Conversation conversation);
        Task UpdateConversationAsync(Conversation conversation);
        Task DeleteConversationAsync(Guid conversationId);
    }
}
