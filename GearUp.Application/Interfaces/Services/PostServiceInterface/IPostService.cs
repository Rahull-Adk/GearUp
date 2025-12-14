using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Domain.Entities.Posts;

namespace GearUp.Application.Interfaces.Services.PostServiceInterface
{
    public interface IPostService
    {
        Task<Result<PostResponseDto>> GetPostByIdAsync(Guid id, Guid currUserId);
        Task<PageResult<PostResponseDto>> GetAllPostsAsync(Guid userId, int pageNum);
        Task<Result<PostResponseDto>> CreatePostAsync(CreatePostRequestDto req, Guid dealerId);
        Task<Result<string>> UpdatePostAsync(Guid id, string updatedContent);
        Task<Result<bool>> DeletePostAsync(Guid id);

       
    }
}
