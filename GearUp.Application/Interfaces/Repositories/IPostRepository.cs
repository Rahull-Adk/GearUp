using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Domain.Entities.Posts;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface IPostRepository
    {
        Task AddPostAsync(Post post);
        Task<PageResult<Post>> GetAllPostsAsync(int pageNum);
        Task<Dictionary<Guid, PostCountsDto>> GetCountsForPostsById(List<Guid> postIds, Guid userId);
        Task<PostResponseDto?> GetPostByIdAsync(Guid postId);
        Task<PostCountsDto> GetCountsForPostById(Guid postId, Guid userId);

        Task<int> GetPostViewCountAsync(Guid postId);
        Task<Post?> GetPostEntityByIdAsync(Guid postId);
    }
}
