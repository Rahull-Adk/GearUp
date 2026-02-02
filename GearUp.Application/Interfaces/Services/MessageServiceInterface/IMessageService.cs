using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.ServiceDtos.Message;

namespace GearUp.Application.Interfaces.Services.MessageServiceInterface
{
    public interface IMessageService
    {
        Task<Result<MessageResponseDto>> SendMessageAsync(SendMessageRequestDto dto, Guid senderId);
        Task<Result<CursorPageResult<ConversationResponseDto>>> GetConversationsAsync(Guid userId, string? cursor);
        Task<Result<ConversationDetailResponseDto>> GetConversationAsync(Guid conversationId, Guid userId, string? cursor);
        Task<Result<ConversationDetailResponseDto>> GetOrCreateConversationWithUserAsync(Guid currentUserId, Guid otherUserId);
        Task<Result<bool>> MarkConversationAsReadAsync(Guid conversationId, Guid userId);
    }
}
