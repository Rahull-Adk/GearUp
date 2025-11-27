
using GearUp.Domain.Entities.Users;

namespace GearUp.Domain.Entities.Chats
{
    public class Message
    {
        public Guid Id { get; private set; }
        public Guid ConversationId { get; private set; }
        public Guid SenderId { get; private set; }
        public Guid ReceiverId { get; private set; }
        public string Content { get; private set; }
        public DateTime SentAt { get; private set; }
        public bool IsRead { get; private set; }
        public User? Sender { get; private set; }
        public User? Receiver { get; private set; }
        public Conversation Conversation { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
        private Message() { }
        public Message(Guid conversationId, Guid senderId, Guid receiverId, string content)
        {
            Id = Guid.NewGuid();
            ConversationId = conversationId;
            SenderId = senderId;
            ReceiverId = receiverId;
            Content = content;
            SentAt = DateTime.UtcNow;
            IsRead = false;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
        public static Message CreateMessage(Guid conversationId, Guid senderId, Guid receiverId, string content)
        {
            return new Message(conversationId, senderId, receiverId, content);
        }
        public void MarkAsRead()
        {
            IsRead = true;
        }
    }
}
