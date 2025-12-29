
using GearUp.Domain.Entities.Posts;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface ILikeRepository
    {
        Task AddPostLikeAsync(PostLike pl);
        void RemovePostLike(Guid userId, Guid postId);
        Task<int> GetPostLikeCountAsync(Guid postId);
        Task AddCommentLikeAsync(CommentLike cl);
        void RemoveCommentLike(Guid userId, Guid commentId);
    }
}
