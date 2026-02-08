

using GearUp.Domain.Entities.Users;
using GearUp.Domain.Enums;

namespace GearUp.Domain.Entities.RealTime
{
    public class Notification
    {
        public Guid Id { get; private set; }
        public string Title { get; private set; }
        public string Content { get; private set; }
        public NotificationEnum NotificationType { get; private set; } = NotificationEnum.Default;
        public Guid ReceiverUserId { get; private set; }
        public Guid ActorUserId { get; private set; }
        public bool IsRead { get; private set; }
        public User? ReceiverUser { get; private set; }
        public User? ActorUser { get; private set; }
        public Guid? PostId { get; private set; }
        public Guid? CommentId { get; private set; }
        public Guid? KycId { get; private set; }
        public Guid? AppointmentId { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private Notification() { }


        public Notification(Guid id)
        {
            Id = id;
            IsRead = false;
            CreatedAt = DateTime.UtcNow;
        }

        public static Notification CreateNotification(
            string title,
            NotificationEnum notificationType,
            Guid actorUserId,
            Guid receiverUserId,
            Guid? postId = null,
            Guid? commentId = null,
            Guid? appointmentId = null
        )
        {
            return new Notification
            {
                Id = Guid.NewGuid(),
                Title = title,
                NotificationType = notificationType,
                ActorUserId = actorUserId,
                ReceiverUserId = receiverUserId,
                PostId = postId,
                CommentId = commentId,
                AppointmentId = appointmentId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
        }


        public void MarkAsRead()
        {
            IsRead = true;
        }

    }
}
