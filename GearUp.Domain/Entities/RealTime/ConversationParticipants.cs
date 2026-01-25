using GearUp.Domain.Entities.Users;

namespace GearUp.Domain.Entities.RealTime
{
    public class ConversationParticipant
    {
        // Composite key: (ConversationId, UserId)
        public Guid ConversationId { get; private set; }
        public Guid UserId { get; private set; }

        public DateTime JoinedAt { get; private set; }
        public DateTime? LastReadAt { get; private set; }

        // Navigation
        public Conversation Conversation { get; private set; } = null!;
        public User? User { get; private set; }

        private ConversationParticipant() { }

        private ConversationParticipant(Guid conversationId, Guid userId)
        {
            ConversationId = conversationId;
            UserId = userId;
            JoinedAt = DateTime.UtcNow;
        }

        public static ConversationParticipant Create(Guid conversationId, Guid userId)
            => new(conversationId, userId);

        public void MarkRead(DateTime readAtUtc)
        {
            if (LastReadAt is null || readAtUtc > LastReadAt.Value)
                LastReadAt = readAtUtc;
        }
    }
}