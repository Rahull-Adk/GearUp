
using GearUp.Domain.Entities.RealTime;
using GearUp.Domain.Entities.Users;

namespace GearUp.Domain.Entities.Chats
{
    public class Conversation
    {
        public Guid Id { get; private set; }
        public Guid? LastMessageId { get; private set; }
        public Message? LastMessage { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private readonly List<ConversationParticipant> _participants = new();
        public IReadOnlyCollection<ConversationParticipant> Participants => _participants.AsReadOnly();

        private readonly List<Message> _messages = new();
        public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

        private Conversation() { }

        public static Conversation Create()
        {
            return new Conversation
            {
                Id = Guid.NewGuid(),
                UpdatedAt = DateTime.UtcNow
            };
        }
    }

}
