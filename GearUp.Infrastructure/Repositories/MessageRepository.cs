using GearUp.Application.Common.Pagination;
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
        private const int PageSize = 20;

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

        public async Task<CursorPageResult<ConversationResponseDto>> GetUserConversationsAsync(Guid userId, Cursor? cursor)
        {
            IQueryable<Conversation> query = _db.Conversations
                .AsNoTracking()
                .Include(c => c.Participants)
                    .ThenInclude(p => p.User)
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .Where(c => c.Participants.Any(p => p.UserId == userId));

            if (cursor is not null)
            {
                query = query.Where(c => (c.LastMessageAt ?? c.CreatedAt) < cursor.CreatedAt ||
                    ((c.LastMessageAt ?? c.CreatedAt) == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) < 0));
            }

            query = query.OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ThenByDescending(c => c.Id);

            var conversations = await query.Take(PageSize + 1).ToListAsync();
            var pageConversations = conversations.Take(PageSize).ToList();
            var pageConversationIds = pageConversations.Select(c => c.Id).ToList();

            var unreadCounts = await (
                from m in _db.Messages.AsNoTracking()
                join p in _db.ConversationParticipants.AsNoTracking()
                    on m.ConversationId equals p.ConversationId
                where pageConversationIds.Contains(m.ConversationId)
                      && p.UserId == userId
                      && m.SenderId != userId
                      && (p.LastReadAt == null || m.SentAt > p.LastReadAt)
                group m by m.ConversationId
                into g
                select new
                {
                    ConversationId = g.Key,
                    Count = g.Count()
                }).ToDictionaryAsync(x => x.ConversationId, x => x.Count);

            var result = new List<ConversationResponseDto>();

            foreach (var conv in pageConversations)
            {
                var otherParticipant = conv.Participants.FirstOrDefault(p => p.UserId != userId);
                if (otherParticipant?.User == null) continue;

                var lastMessage = conv.Messages.FirstOrDefault();
                unreadCounts.TryGetValue(conv.Id, out var unreadCount);

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

            bool hasMore = conversations.Count > PageSize;
            string? nextCursor = null;

            if (hasMore && result.Count > 0)
            {
                var lastItem = result.Last();
                nextCursor = Cursor.Encode(new Cursor
                {
                    CreatedAt = lastItem.LastMessageAt ?? lastItem.CreatedAt,
                    Id = lastItem.Id
                });
            }

            return new CursorPageResult<ConversationResponseDto>
            {
                Items = result,
                NextCursor = nextCursor,
                HasMore = hasMore
            };
        }

        public async Task<CursorPageResult<Message>> GetConversationMessagesAsync(Guid conversationId, Cursor? cursor)
        {
            IQueryable<Message> query = _db.Messages
                .AsNoTracking()
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.SentAt)
                .ThenByDescending(m => m.Id);

            if (cursor is not null)
            {
                query = query.Where(m => m.SentAt < cursor.CreatedAt ||
                    (m.SentAt == cursor.CreatedAt && m.Id.CompareTo(cursor.Id) < 0));
            }

            var messages = await query.Take(PageSize + 1).ToListAsync();

            bool hasMore = messages.Count > PageSize;
            string? nextCursor = null;

            if (hasMore)
            {
                var lastItem = messages[PageSize - 1];
                nextCursor = Cursor.Encode(new Cursor
                {
                    CreatedAt = lastItem.SentAt,
                    Id = lastItem.Id
                });
            }

            return new CursorPageResult<Message>
            {
                Items = messages.Take(PageSize).ToList(),
                NextCursor = nextCursor,
                HasMore = hasMore
            };
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
