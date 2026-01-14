using GearUp.Domain.Entities.Posts;

namespace GearUp.Application.ServiceDtos.Post
{
    public class UpdatePostDto
    {
        public string Caption { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public PostVisibility Visibility { get; set; }
    }
}