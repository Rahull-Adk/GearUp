using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.ServiceDtos.Message;
using GearUp.Domain.Entities.RealTime;
using GearUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GearUp.Infrastructure.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly GearUpDbContext _db;

        public MessageRepository(GearUpDbContext db)
        {
            _db = db;
        }

        public async Task<Conversation?> GetConversationByIdAsync(Guid conversationId)
        {
            return await _db.Conversations
                .Include(c => c.Participants)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(c => c.Id == conversationId);
        }

        public async Task<Conversation?> GetConversationByParticipantsAsync(Guid userId1, Guid userId2)
        {
            return await _db.Conversations
                .Include(c => c.Participants)
                    .ThenInclude(p => p.User)
                .Where(c => c.Participants.Any(p => p.UserId == userId1) &&
                            c.Participants.Any(p => p.UserId == userId2))
                .FirstOrDefaultAsync();
        }

        public async Task<List<ConversationResponseDto>> GetUserConversationsAsync(Guid userId)
        {
            var conversations = await _db.Conversations
                .AsNoTracking()
                .Include(c => c.Participants)
                    .ThenInclude(p => p.User)
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .Where(c => c.Participants.Any(p => p.UserId == userId))
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToListAsync();

            var result = new List<ConversationResponseDto>();

            foreach (var conv in conversations)
            {
                var otherParticipant = conv.Participants.FirstOrDefault(p => p.UserId != userId);
                if (otherParticipant?.User == null) continue;

                var myParticipant = conv.Participants.First(p => p.UserId == userId);
                var lastMessage = conv.Messages.FirstOrDefault();

                // Count unread messages (messages sent after my last read time)
                var unreadCount = await _db.Messages
                    .CountAsync(m => m.ConversationId == conv.Id &&
                                     m.SenderId != userId &&
                                     (myParticipant.LastReadAt == null || m.SentAt > myParticipant.LastReadAt));

                result.Add(new ConversationResponseDto
                {
                    Id = conv.Id,
                    OtherUserId = otherParticipant.UserId,
                    OtherUserName = otherParticipant.User.Name,
                    OtherUserAvatarUrl = otherParticipant.User.AvatarUrl,
                    LastMessageText = lastMessage?.Text,
                    LastMessageAt = conv.LastMessageAt,
                    UnreadCount = unreadCount,
                    CreatedAt = conv.CreatedAt
                });
            }

            return result;
        }

        public async Task<List<Message>> GetConversationMessagesAsync(Guid conversationId, int page = 1, int pageSize = 50)
        {
            return await _db.Messages
                .AsNoTracking()
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> IsParticipantInConversationAsync(Guid conversationId, Guid userId)
        {
            return await _db.ConversationParticipants
                .AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId);
        }

        public async Task AddConversationAsync(Conversation conversation)
        {
            await _db.Conversations.AddAsync(conversation);
        }

        public async Task AddMessageAsync(Message message)
        {
            await _db.Messages.AddAsync(message);
        }

        public async Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId)
        {
            var participant = await _db.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId);

            if (participant == null) return 0;

            return await _db.Messages
                .CountAsync(m => m.ConversationId == conversationId &&
                                 m.SenderId != userId &&
                                 (participant.LastReadAt == null || m.SentAt > participant.LastReadAt));
        }

        public async Task MarkMessagesAsReadAsync(Guid conversationId, Guid userId)
        {
            var participant = await _db.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId);

            participant?.MarkRead(DateTime.UtcNow);
        }
    }
}
