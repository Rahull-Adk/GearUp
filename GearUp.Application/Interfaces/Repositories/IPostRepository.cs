using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Application.ServiceDtos.Socials;
using GearUp.Domain.Entities.Posts;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface IPostRepository
    {
        Task AddPostAsync(Post post);
        Task<PageResult<PostResponseDto>> GetLatestFeedAsync(int pageNum, Guid currUserId);
        Task<PostResponseDto?> GetPostByIdAsync(Guid postId, Guid currUserId);
        Task<PageResult<PostResponseDto?>> GetAllUserPostByUserIdAsync(Guid currUserId, int pageNum);
        Task<PostCountsDto> GetCountsForPostById(Guid postId, Guid userId);
        Task<int> GetPostViewCountAsync(Guid postId);
        Task<bool> PostExistAsync(Guid PostId);
        Task<Post?> GetPostEntityByIdAsync(Guid postId);
        Task<PageResult<UserEngagementDto>> GetPostLikersAsync(Guid postId, int pageNum);
    }
}
