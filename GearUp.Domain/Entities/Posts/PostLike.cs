using System.Text.Json.Serialization;
using GearUp.Domain.Entities.Users;

namespace GearUp.Domain.Entities.Posts
{
    public class PostLike
    {
        public Guid Id { get; private set; }
        public Guid PostId { get; private set; }
        [JsonIgnore]
        public Post? Post { get; private set; }
        public Guid LikedUserId { get; private set; }
        [JsonIgnore]
        public User? LikedUser { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private PostLike()
        {
            
        }

        public static PostLike CreateLike(Guid postId, Guid likedUserId)
        {
            return new PostLike
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                LikedUserId = likedUserId,
                UpdatedAt = DateTime.UtcNow
            };
        }

    }
}
