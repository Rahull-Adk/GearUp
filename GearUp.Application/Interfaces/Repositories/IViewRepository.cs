using GearUp.Domain.Entities.Posts;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface IViewRepository
    {
        Task CreatePostViewAsync(PostView postView);
        Task<bool> HasViewTimeElapsedAsync(Guid postId, Guid userId);
    }
}