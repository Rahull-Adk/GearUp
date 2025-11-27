

using GearUp.Domain.Entities.Users;

namespace GearUp.Domain.Entities.RealTime
{
    public class Notification
    {
        public Guid Id { get; private set; }
        public string Title { get; private set; }
        public string Content { get; private set; }
        public Guid UserId { get; private set; }
        public bool IsRead { get; private set; }
        public User? User { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private Notification() { }

        public Notification(Guid id, string title, string content, Guid userId, bool isRead, User user)
        {
            Id = id;
            Title = title;
            Content = content;
            UserId = userId;
            IsRead = isRead;
            User = user;
            CreatedAt = DateTime.UtcNow; 
        }

        public static Notification CreateNotification(string title, string content, Guid userId, User user)
        {
            return new Notification(Guid.NewGuid(), title, content, userId, false, user);
        }

        public void MarkAsRead()
        {
            IsRead = true;
        }

    }
}
