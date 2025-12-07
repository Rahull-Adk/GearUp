using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GearUp.Domain.Entities.Users;

namespace GearUp.Domain.Entities.Posts
{
    public class CommentLike
    {
        public Guid Id { get; set; }
        public Guid CommentId { get; set; }
        public PostComment Comment { get; set; } = null!;
        public Guid LikedUserId { get; set; }
        public User LikedUser { get; set; } = null!;
        public DateTime CreatedAt { get; set; }


        private CommentLike()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }

        public static CommentLike CreateCommentLike(Guid commentId, Guid likedUserId)
        {
            return new CommentLike
            {
                CommentId = commentId,
                LikedUserId = likedUserId,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}