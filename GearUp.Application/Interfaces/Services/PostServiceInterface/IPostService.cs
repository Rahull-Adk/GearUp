using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Application.ServiceDtos.Socials;
using GearUp.Domain.Entities.Posts;

namespace GearUp.Application.Interfaces.Services.PostServiceInterface
{
    public interface IPostService
    {
        Task<Result<PostResponseDto>> GetPostByIdAsync(Guid id, Guid currUserId);
        Task<Result<CursorPageResult<PostResponseDto>>> GetLatestFeedAsync(Guid userId, string cursor);
        Task<Result<CursorPageResult<PostResponseDto?>>> GetMyPosts(Guid userId, string? cursor);
        Task<Result<PostResponseDto>> CreatePostAsync(CreatePostRequestDto req, Guid dealerId);
        Task<Result<CursorPageResult<UserEngagementDto>>> GetPostLikersAsync(Guid postId, string? cursor);
        Task<Result<string>> UpdatePostAsync(Guid id, Guid currUserId, UpdatePostDto dto);
        Task<Result<bool>> DeletePostAsync(Guid id, Guid currUserId);


    }
}
