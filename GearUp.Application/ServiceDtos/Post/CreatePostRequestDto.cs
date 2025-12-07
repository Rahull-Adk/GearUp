using GearUp.Domain.Entities.Posts;

namespace GearUp.Application.ServiceDtos.Post
{
    public class CreatePostRequestDto
    {
        public string Caption { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public PostVisibility Visibility { get; set; } = PostVisibility.Default;
        public Guid CarId { get; set; }
    }
}
