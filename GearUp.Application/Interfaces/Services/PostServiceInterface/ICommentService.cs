using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.Post;

namespace GearUp.Application.Interfaces.Services.PostServiceInterface
{
    public interface ICommentService
    {
        Task<Result<CommentDto>> PostCommentAsync(CreateCommentDto comment, Guid userId);
        Task<Result<CommentDto>> UpdateCommentAsync(Guid commentId, Guid userId, string updatedContent);
        Task<Result<bool>> DeleteCommentAsync(Guid commentId, Guid userId);
    }
}
