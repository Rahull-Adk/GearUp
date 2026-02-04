using GearUp.Domain.Entities.Cars;
using GearUp.Domain.Entities.Users;

namespace GearUp.Domain.Entities.Posts
{
    public class Post
    {
        public Guid Id { get; private set; }
        public string Caption { get; private set; }
        public string Content { get; private set; }
        public PostVisibility Visibility { get; private set; }
        public Guid UserId { get; private set; }
        public Guid? CarId { get; private set; }
        public User? User { get; private set; }
        public Car? Car { get; private set; }

        // Denormalized counts for performance
        public int LikeCount { get; private set; }
        public int CommentCount { get; private set; }
        public int ViewCount { get; private set; }

        private readonly List<PostLike> _likes = new List<PostLike>();
        private readonly List<PostComment> _comments = new List<PostComment>();
        private readonly List<PostView> _views = new List<PostView>();
        public IReadOnlyCollection<PostLike> Likes => _likes.AsReadOnly();
        public IReadOnlyCollection<PostComment> Comments => _comments.AsReadOnly();
        public IReadOnlyCollection<PostView> Views => _views.AsReadOnly();
        public bool IsDeleted { get; private set; } = false;
        public DateTime? DeletedAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }


        private Post()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public static Post CreatePost(
            string caption,
            string content,
            PostVisibility visibility,
            Guid userId,
            Guid carId)
        {
            return new Post
            {
                Caption = caption,
                Content = content,
                Visibility = visibility,
                UserId = userId,
                CarId = carId,
                LikeCount = 0,
                CommentCount = 0,
                ViewCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public void SoftDelete()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void IncrementLikeCount()
        {
            LikeCount++;
            UpdatedAt = DateTime.UtcNow;
        }

        public void DecrementLikeCount()
        {
            if (LikeCount > 0) LikeCount--;
            UpdatedAt = DateTime.UtcNow;
        }

        public void IncrementCommentCount()
        {
            CommentCount++;
            UpdatedAt = DateTime.UtcNow;
        }

        public void DecrementCommentCount()
        {
            if (CommentCount > 0) CommentCount--;
            UpdatedAt = DateTime.UtcNow;
        }

        public void IncrementViewCount()
        {
            ViewCount++;
        }

        public void UpdateContent(string caption, string content, PostVisibility visibility)
        {
            if (!string.IsNullOrWhiteSpace(content))
                Content = content;
            if (!string.IsNullOrWhiteSpace(caption))
                Caption = caption;
            if (visibility != PostVisibility.Default)
                Visibility = visibility;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public enum PostVisibility
    {
        Default = 0,
        Public = 1,
        Private = 2,
    }
}
