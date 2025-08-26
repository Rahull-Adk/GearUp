using GearUp.Domain.Entities.Posts;

namespace GearUp.Domain.Repository_Interfaces
{
    public interface IPostRepository
    {
        Task<Post?> GetPostByIdAsync(Guid postId);
        Task<IEnumerable<Post>> GetAllPostsAsync();
        Task<IEnumerable<Post>> GetPostsByUserIdAsync(Guid userId);
        Task AddPostAsync(Post post);
        Task UpdatePostAsync(Post post);
        Task DeletePostAsync(Guid postId);
    }
}
