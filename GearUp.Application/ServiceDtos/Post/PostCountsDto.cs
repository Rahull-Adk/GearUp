using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GearUp.Application.ServiceDtos.Post
{
    public class PostCountsDto
    {
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public int ViewCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
    }

}
