using GearUp.Domain.Entities.Posts;

namespace GearUp.Application.ServiceDtos.Post
{
    public class CommentDto
    {
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
        public Guid CommentedUserId { get; set; }
        public string CommentedUserName { get; set; } = string.Empty;
        public int LikeCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public bool IsEdited { get; set; }
        public string CommentedUserProfilePictureUrl { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
        public Guid? ParentCommentId { get; set; }
        public List<CommentDto> Replies { get; set; } = [];
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
