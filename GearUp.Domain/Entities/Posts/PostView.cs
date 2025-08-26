using GearUp.Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GearUp.Domain.Entities.Posts
{
    public class PostView
    {
        public Guid Id { get; private set; }
        public Guid PostId { get; private set; }
        public Post Post { get; private set; }
        public Guid ViewedUserId { get; private set; }
        public User ViewedUser { get; private set; }
        public DateTime ViewedAt { get; private set; }

        private PostView()
        {
        }
        public static PostView CreatePostView(Guid postId, Guid viewedUserId)
        {
            return new PostView
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                ViewedUserId = viewedUserId,
                ViewedAt = DateTime.UtcNow
            };
        }
    }
}
