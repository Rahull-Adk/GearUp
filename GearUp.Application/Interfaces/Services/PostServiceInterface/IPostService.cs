using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Application.ServiceDtos.Socials;
using GearUp.Domain.Entities.Posts;

namespace GearUp.Application.Interfaces.Services.PostServiceInterface
{
    public interface IPostService
    {
        Task<Result<PostResponseDto>> GetPostByIdAsync(Guid id, Guid currUserId);
        Task<Result<PageResult<PostResponseDto>>> GetAllPostsAsync(Guid userId, int pageNum);
        Task<Result<PostResponseDto>> CreatePostAsync(CreatePostRequestDto req, Guid dealerId);
        Task<Result<PageResult<UserEngagementDto>>> GetPostLikersAsync(Guid postId, int pageNum);
        Task<Result<string>> UpdatePostAsync(Guid id, string updatedContent);
        Task<Result<bool>> DeletePostAsync(Guid id, Guid currUserId);


    }
}
