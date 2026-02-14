using GearUp.Domain.Enums;

namespace GearUp.Application.ServiceDtos
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public NotificationEnum NotificationType { get; set; } = NotificationEnum.Default;
        public Guid ReceiverUserId { get; set; }
        public Guid ActorUserId { get; set; }
        public bool IsRead { get; set; } = false;
        public Guid? PostId { get; set; }
        public Guid? CommentId { get; set; }
        public Guid? KycId { get; set; }
        public Guid? CarId { get; set; }
        public Guid? AppointmentId { get; set; }
        public DateTime SentAt { get; set; }
    }
}