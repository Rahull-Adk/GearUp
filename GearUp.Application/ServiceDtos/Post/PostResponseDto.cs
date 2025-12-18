using GearUp.Application.ServiceDtos.Car;
using GearUp.Domain.Entities.Posts;

namespace GearUp.Application.ServiceDtos.Post
{
    public class PostResponseDto
    {
        public Guid Id { get; set; }
        public string Caption { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string AuthorUsername { get; set; } = string.Empty;
        public string AuthorAvatarUrl { get; set; } = string.Empty;
        public PostVisibility Visibility { get; set; }
        public CarResponseDto? CarDto { get; set; } = new CarResponseDto();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int LikeCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public int CommentCount { get; set; }
        public int ViewCount { get; set; }
    }
}
