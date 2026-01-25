namespace GearUp.Application.ServiceDtos.Message
{
    public class SendMessageRequestDto
    {
        public Guid ReceiverId { get; set; }
        public string? Text { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class MessageResponseDto
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string? SenderAvatarUrl { get; set; }
        public string? Text { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool IsMine { get; set; }
    }

    public class ConversationResponseDto
    {
        public Guid Id { get; set; }
        public Guid OtherUserId { get; set; }
        public string OtherUserName { get; set; } = string.Empty;
        public string? OtherUserAvatarUrl { get; set; }
        public string? LastMessageText { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ConversationDetailResponseDto
    {
        public Guid ConversationId { get; set; }
        public Guid OtherUserId { get; set; }
        public string OtherUserName { get; set; } = string.Empty;
        public string? OtherUserAvatarUrl { get; set; }
        public List<MessageResponseDto> Messages { get; set; } = new();
    }
}
