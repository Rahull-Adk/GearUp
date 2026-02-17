using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Application.ServiceDtos.Socials;

namespace GearUp.Application.Interfaces.Services.PostServiceInterface
{
    public interface ICommentService
    {
        Task<Result<CommentDto>> PostCommentAsync(CreateCommentDto comment, Guid userId);
        Task<Result<IEnumerable<CommentDto>>> GetParentCommentsByPostId(Guid postId, Guid userId);
        Task<Result<IEnumerable<CommentDto>>> GetChildCommentsByParentId(Guid parentCommentId, Guid userId);
        Task<Result<CommentDto>> UpdateCommentAsync(Guid commentId, Guid userId, string updatedContent);
        Task<Result<bool>> DeleteCommentAsync(Guid commentId, Guid userId);
        Task<Result<CursorPageResult<UserEngagementDto>>> GetCommentLikersAsync(Guid commentId, string? cursor);
    }
}
