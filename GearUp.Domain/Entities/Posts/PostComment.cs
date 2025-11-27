using GearUp.Domain.Entities.Users;

namespace GearUp.Domain.Entities.Posts
{
    public class PostComment
    {
        public Guid Id { get; private set; }
        public Guid PostId { get; private set; }
        public Post? Post { get; private set; }
        public Guid CommentedUserId { get; private set; }
        public User? CommentedUser { get; private set; }
        public string Content { get; private set; }
        public Guid? ParentCommentId { get; private set; }
        public PostComment ParentComment { get; private set; }
        public bool IsDeleted { get; private set; }
        private readonly List<PostComment> _replies = new List<PostComment>();
        public IReadOnlyCollection<PostComment> Replies => _replies.AsReadOnly();
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }


        private PostComment(Guid postId, Guid commentedUserId, string content, Guid? parentCommentId)
        {
            Id = Guid.NewGuid();
            PostId = postId;
            CommentedUserId = commentedUserId;
            Content = content;
            ParentCommentId = parentCommentId;
            IsDeleted = false;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public static PostComment CreateComment(Guid postId, Guid commentedUserId, string content, Guid? parentCommentId = null)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Content cannot be null or empty.", nameof(content));
            return new PostComment(postId, commentedUserId, content, parentCommentId);
        }

        public void DeleteComment()
        {
            IsDeleted = true;
            UpdatedAt = DateTime.UtcNow;
            
        }

        public void UpdateContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Content cannot be null or empty.", nameof(content));
            Content = content;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
