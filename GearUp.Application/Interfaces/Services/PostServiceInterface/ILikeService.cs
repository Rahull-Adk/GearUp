using GearUp.Application.Common;

namespace GearUp.Application.Interfaces.Services.PostServiceInterface
{
    public interface ILikeService
    {
        // Post likes
        Task<Result<int>> LikePostAsync(Guid postId, Guid userId);
        Task<Result<int>> UnlikePostAsync(Guid postId, Guid userId);

        // Comment likes
        Task<Result<int>> LikeCommentAsync(Guid commentId, Guid userId);
        Task<Result<int>> UnlikeCommentAsync(Guid commentId, Guid userId);
    }
}
