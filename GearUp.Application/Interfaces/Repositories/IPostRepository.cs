using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Domain.Entities.Posts;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface IPostRepository
    {
        Task AddPostAsync(Post post);
        Task<PageResult<PostResponseDto>> GetAllPostsAsync(int pageNum, Guid currUserId);
        Task<PostResponseDto?> GetPostByIdAsync(Guid postId, Guid currUserId);
        Task<PostCountsDto> GetCountsForPostById(Guid postId, Guid userId);
        Task<int> GetPostViewCountAsync(Guid postId);
        Task<Post?> GetPostEntityByIdAsync(Guid postId);
    }
}
