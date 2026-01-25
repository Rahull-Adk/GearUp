namespace GearUp.Domain.Entities.RealTime
{
    public class Conversation
    {
        public Guid Id { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public Guid? LastMessageId { get; private set; }
        public DateTime? LastMessageAt { get; private set; }

        public ICollection<Message> Messages { get; private set; } = new List<Message>();
        public ICollection<ConversationParticipant> Participants { get; private set; } = new List<ConversationParticipant>();

        private Conversation() { }

        private Conversation(Guid id)
        {
            Id = id;
            CreatedAt = DateTime.UtcNow;
        }

        public static Conversation Create()
            => new(Guid.NewGuid());

        public void AddParticipant(Guid userId)
        {
            if (Participants.All(p => p.UserId != userId))
            {
                Participants.Add(ConversationParticipant.Create(Id, userId));
            }
        }

        public void TouchLastMessage(Guid messageId, DateTime sentAt)
        {
            LastMessageId = messageId;
            LastMessageAt = sentAt;
        }
    }
}