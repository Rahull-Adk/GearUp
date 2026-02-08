using GearUp.Application.Common.Pagination;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Application.ServiceDtos.Socials;
using GearUp.Domain.Entities.Posts;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface ICommentRepository
    {
        Task AddCommentAsync(PostComment comment);
        Task<bool> CommentExistAsync(Guid commentId);
        Task<IEnumerable<CommentDto>> GetTopLevelCommentsByPostIdAsync(Guid postId, Guid userId);
        Task<int> GetCommentLikeCountByIdAsync(Guid commentId);
        Task<IEnumerable<CommentDto>> GetChildCommentsByParentIdAsync(Guid parentCommentId, Guid userId);
        Task<PostComment?> GetCommentByIdAsync(Guid commentId);
        Task<bool> IsCommentAlreadyLikedByUserAsync(Guid commentId, Guid userId);
        Task<CursorPageResult<UserEngagementDto>> GetCommentLikersAsync(Guid commentId, Cursor? cursor, int pageSize = 20);
    }
}
