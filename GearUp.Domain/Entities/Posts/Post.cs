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
        public User User { get; private set; }
        public Car Car { get; private set; }

        private readonly List<PostLike> _likes = new List<PostLike>();
        private readonly List<PostComment> _comments = new List<PostComment>();
        private readonly List<PostView> _views = new List<PostView>();
        public IReadOnlyCollection<PostLike> Likes => _likes.AsReadOnly();
        public IReadOnlyCollection<PostComment> Comments => _comments.AsReadOnly();
        public IReadOnlyCollection<PostView> Views => _views.AsReadOnly();



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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }

    public enum PostVisibility
    {
        Default = 0,
        Public = 1,
        Private = 2,
    }
}
