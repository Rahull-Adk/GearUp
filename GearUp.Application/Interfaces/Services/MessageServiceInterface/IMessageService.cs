using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.Message;

namespace GearUp.Application.Interfaces.Services.MessageServiceInterface
{
    public interface IMessageService
    {
        Task<Result<MessageResponseDto>> SendMessageAsync(SendMessageRequestDto dto, Guid senderId);
        Task<Result<List<ConversationResponseDto>>> GetConversationsAsync(Guid userId);
        Task<Result<ConversationDetailResponseDto>> GetConversationAsync(Guid conversationId, Guid userId, int page = 1, int pageSize = 50);
        Task<Result<ConversationDetailResponseDto>> GetOrCreateConversationWithUserAsync(Guid currentUserId, Guid otherUserId);
        Task<Result<bool>> MarkConversationAsReadAsync(Guid conversationId, Guid userId);
    }
}
