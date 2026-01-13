using GearUp.Application.ServiceDtos.Post;
using GearUp.Domain.Entities.Posts;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface ICommentRepository
    {
        Task AddCommentAsync(PostComment comment);
        Task<bool> CommentExistAsync(Guid commentId);
        Task<IEnumerable<CommentDto>> GetTopLevelCommentsByPostIdAsync(Guid postId);
        Task<int> GetCommentLikeCountByIdAysnc(Guid commentId);
        Task<IEnumerable<CommentDto>> GetChildCommentsByParentIdAsync(Guid parentCommentId);
        Task<PostComment?> GetCommentByIdAsync(Guid commentId);
        Task<bool> IsCommentAlreadyLikedByUserAsync(Guid commentId, Guid userId);
    }
}
