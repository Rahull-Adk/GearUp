using GearUp.Domain.Entities.Chats;
using GearUp.Domain.Entities.Users;


namespace GearUp.Domain.Entities.RealTime
{
    public class ConversationParticipant
    {
        public Guid UserId { get; private set; }
        public Guid ConversationId { get; private set; }

        
        public User User { get; private set; }
        public Conversation Conversation { get; private set; }

        public DateTime JoinedAt { get; private set; }
        public bool IsMuted { get; private set; }

        private ConversationParticipant() { }

        public ConversationParticipant(Guid userId, Guid conversationId)
        {
            UserId = userId;
            ConversationId = conversationId;
            JoinedAt = DateTime.UtcNow;
            IsMuted = false;
        }
    }

}
