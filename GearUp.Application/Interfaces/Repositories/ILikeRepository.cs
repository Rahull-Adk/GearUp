
using GearUp.Domain.Entities.Posts;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface ILikeRepository
    {
        // Post likes
        Task<bool> AddPostLikeAsync(PostLike pl);
        Task<bool> RemovePostLikeAsync(Guid userId, Guid postId);
        Task<int> GetPostLikeCountAsync(Guid postId);

        // Comment likes
        Task<bool> AddCommentLikeAsync(CommentLike cl);
        Task<bool> RemoveCommentLikeAsync(Guid userId, Guid commentId);
    }
}
