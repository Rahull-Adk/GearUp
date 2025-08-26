using GearUp.Domain.Entities.Users;

namespace GearUp.Domain.Entities.Posts
{
    public class PostLike
    {
        public Guid Id { get; private set; }
        public Guid PostId { get; private set; }
        public Post Post { get; private set; }
        public Guid LikedUserId { get; private set; }
        public User LikedUser { get; private set; }
        public bool IsLiked { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private PostLike()
        {
            Id = Guid.NewGuid();
            UpdatedAt = DateTime.UtcNow;
        }

        public static PostLike CreateLike(Guid postId, Guid likedUserId)
        {
            return new PostLike
            {
                PostId = postId,
                LikedUserId = likedUserId,
                IsLiked = true,
            };
        }

        public void ToggleLike()
        {
            IsLiked = !IsLiked;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
