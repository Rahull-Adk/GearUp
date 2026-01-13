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
            var views = await _db.PostViews.Where(pv => pv.PostId == postId && pv.ViewedUserId == userId).ToListAsync();

            if (views.Count == 0)
                return true;
            var lastestView = views.OrderByDescending(pv => pv.ViewedAt).First();

            return (DateTime.UtcNow - lastestView.ViewedAt).TotalMinutes >= 60;

        }
    }
}