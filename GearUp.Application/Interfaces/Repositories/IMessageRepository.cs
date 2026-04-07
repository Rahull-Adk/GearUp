using GearUp.Application.Common.Pagination;
using GearUp.Application.ServiceDtos.Message;
using GearUp.Domain.Entities.RealTime;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface IMessageRepository
    {
        Task<Conversation?> GetConversationByIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
        Task<Conversation?> GetConversationByParticipantsAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken = default);
        Task<CursorPageResult<ConversationResponseDto>> GetUserConversationsAsync(Guid userId, Cursor? cursor, CancellationToken cancellationToken = default);
        Task<CursorPageResult<Message>> GetConversationMessagesAsync(Guid conversationId, Cursor? cursor, CancellationToken cancellationToken = default);
        Task<bool> IsParticipantInConversationAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
        Task AddConversationAsync(Conversation conversation);
        Task AddMessageAsync(Message message);
        Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId);
        Task MarkMessagesAsReadAsync(Guid conversationId, Guid userId);
    }
}
