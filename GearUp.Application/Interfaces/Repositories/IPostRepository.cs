using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Application.ServiceDtos.Socials;
using GearUp.Domain.Entities.Posts;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface IPostRepository
    {
        Task AddPostAsync(Post post);
        Task<CursorPageResult<PostResponseDto>> GetLatestFeedAsync(Cursor? c, Guid currUserId);
        Task<PostResponseDto?> GetPostByIdAsync(Guid postId, Guid currUserId);
        Task<CursorPageResult<PostResponseDto?>> GetAllUserPostByUserIdAsync(Cursor? c, Guid currUserId);
        Task<PostCountsDto> GetCountsForPostById(Guid postId, Guid userId);
        Task<int> GetPostViewCountAsync(Guid postId);
        Task<bool> PostExistAsync(Guid PostId);
        Task<Post?> GetPostEntityByIdAsync(Guid postId);
        Task<CursorPageResult<UserEngagementDto>> GetPostLikersAsync(Guid postId, Cursor? cursor);
    }
}
