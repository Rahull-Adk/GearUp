
using GearUp.Domain.Entities.Posts;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface ILikeRepository
    {
        Task AddPostLikeAsync(PostLike pl);
        Task RemovePostLikeAsync(Guid userId, Guid postId);
        Task<int> GetPostLikeCountAsync(Guid postId);
        Task AddCommentLikeAsync(CommentLike cl);
        Task RemoveCommentLikeAsync(Guid userId, Guid commentId);
    }
}
