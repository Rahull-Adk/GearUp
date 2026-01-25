using GearUp.Domain.Entities.Users;

namespace GearUp.Domain.Entities.RealTime
{
    public class Message
    {
        public Guid Id { get; private set; }
        public Guid ConversationId { get; private set; }
        public Guid SenderId { get; private set; }

        public string? Text { get; private set; }
        public string? ImageUrl { get; private set; }

        public DateTime SentAt { get; private set; }
        public DateTime? EditedAt { get; private set; }

        public User? Sender { get; private set; }
        public Conversation Conversation { get; private set; } = null!;

        private Message() { }

        private Message(Guid conversationId, Guid senderId, string? text, string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentException("Message must contain text or image.");

            Id = Guid.NewGuid();
            ConversationId = conversationId;
            SenderId = senderId;

            Text = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();

            SentAt = DateTime.UtcNow;
        }

        public static Message Create(Guid conversationId, Guid senderId, string? text, string? imageUrl = null)
            => new(conversationId, senderId, text, imageUrl);

        public void Edit(string? newText, string? newImageUrl = null)
        {
            if (string.IsNullOrWhiteSpace(newText) && string.IsNullOrWhiteSpace(newImageUrl))
                throw new ArgumentException("Edited message must contain text or image.");

            Text = string.IsNullOrWhiteSpace(newText) ? null : newText.Trim();
            ImageUrl = string.IsNullOrWhiteSpace(newImageUrl) ? null : newImageUrl.Trim();
            EditedAt = DateTime.UtcNow;
        }
    }
}