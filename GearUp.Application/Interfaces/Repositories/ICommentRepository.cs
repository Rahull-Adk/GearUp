using GearUp.Domain.Entities.Posts;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface ICommentRepository
    {
        Task AddCommentAsync(PostComment comment);
        Task<List<PostComment>> GetAllCommentsByPostIdAsync(Guid postId);
        Task<List<PostComment>> GetAllCommentsByPostIdsAsync(List<Guid> postIds);
        Task<Dictionary<Guid, int>> GetCommentsLikeCount(List<Guid> commentIds);
        Task<int> GetCommentLikeCountByIdAysnc(Guid commentId);
        Task<Dictionary<Guid, PostComment>> GetAllCommentsByIdsAsync(List<Guid> commentIds);
        Task<List<Guid>>? GetAllCommentsLikedByUser(Guid userId, List<Guid> commentIds);
        Task<PostComment?> GetCommentByIdAsync(Guid commentId);
        Task<bool> IsCommentAlreadyLikedByUserAsync(Guid commentId, Guid userId);
    }
}
