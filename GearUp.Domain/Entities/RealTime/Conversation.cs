
using GearUp.Domain.Entities.Users;

namespace GearUp.Domain.Entities.Chats
{
    public class Conversation
    {
        public Guid Id { get; private set; }
        public Guid User1Id { get; private set; }
        public Guid User2Id { get; private set; }
        public Guid? LastMessageId { get; private set; }
        public User User1 { get; private set; }
        public User User2 { get; private set; }
        public Message LastMessage { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public Conversation(Guid user1Id, Guid user2Id, Guid lastMessageId)
        {
            Id = Guid.NewGuid();
            User1Id = user1Id;
            User2Id = user2Id;
            LastMessageId = lastMessageId;
            UpdatedAt = DateTime.UtcNow;
        }

        public static Conversation CreateConversation(Guid user1Id, Guid user2Id, Guid lastMessageId)
        {
            return new Conversation(user1Id, user2Id, lastMessageId);
        }
    }
}
