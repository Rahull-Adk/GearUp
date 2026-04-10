using GearUp.Application.Interfaces.Repositories;
using GearUp.Domain.Entities.Posts;
using GearUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GearUp.Infrastructure.Repositories
{
    public class ViewRepository : IViewRepository
    {
        private readonly GearUpDbContext _db;

        public ViewRepository(GearUpDbContext dbContext)
        {
            _db = dbContext;
        }
        public async Task CreatePostViewAsync(PostView postView)
        {
            await _db.PostViews.AddAsync(postView);
        }

        public async Task<bool> HasViewTimeElapsedAsync(Guid postId, Guid userId)
        {
            var latestViewedAt = await _db.PostViews
                .Where(pv => pv.PostId == postId && pv.ViewedUserId == userId)
                .OrderByDescending(pv => pv.ViewedAt)
                .Select(pv => (DateTime?)pv.ViewedAt)
                .FirstOrDefaultAsync();

            if (!latestViewedAt.HasValue)
                return true;

            return (DateTime.UtcNow - latestViewedAt.Value).TotalMinutes >= 60;

        }
    }
}
