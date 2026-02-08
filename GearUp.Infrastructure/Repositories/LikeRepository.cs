using GearUp.Application.Interfaces.Repositories;
using GearUp.Domain.Entities.Posts;
using GearUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GearUp.Infrastructure.Repositories
{
    public class LikeRepository : ILikeRepository
    {
        private readonly GearUpDbContext _db;

        public LikeRepository(GearUpDbContext db)
        {
            _db = db;
        }

        #region Post Likes

        public async Task<bool> AddPostLikeAsync(PostLike pl)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                await _db.PostLikes.AddAsync(pl);
                await _db.SaveChangesAsync();

                await _db.Posts
                    .Where(p => p.Id == pl.PostId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LikeCount, p => p.LikeCount + 1));

                await transaction.CommitAsync();
                return true;
            }
            catch (DbUpdateException ex) when (IsDuplicateKey(ex))
            {
                await transaction.RollbackAsync();
                // Detach the entity to prevent it from being saved again in subsequent SaveChangesAsync calls
                _db.Entry(pl).State = EntityState.Detached;
                return false;
            }
        }

        public async Task<bool> RemovePostLikeAsync(Guid userId, Guid postId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var rowsDeleted = await _db.PostLikes
                    .Where(p => p.PostId == postId && p.LikedUserId == userId)
                    .ExecuteDeleteAsync();

                if (rowsDeleted == 0)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                await _db.Posts
                    .Where(p => p.Id == postId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LikeCount, p => p.LikeCount - 1));

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<int> GetPostLikeCountAsync(Guid postId)
        {
            return await _db.PostLikes.AsNoTracking().CountAsync(pl => pl.PostId == postId);
        }

        #endregion

        #region Comment Likes

        public async Task<bool> AddCommentLikeAsync(CommentLike cl)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                await _db.CommentLikes.AddAsync(cl);
                await _db.SaveChangesAsync();

                await _db.PostComments
                    .Where(c => c.Id == cl.CommentId)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.LikeCount, c => c.LikeCount + 1));

                await transaction.CommitAsync();
                return true;
            }
            catch (DbUpdateException ex) when (IsDuplicateKey(ex))
            {
                await transaction.RollbackAsync();
                // Detach the entity to prevent it from being saved again in subsequent SaveChangesAsync calls
                _db.Entry(cl).State = EntityState.Detached;
                return false;
            }
        }

        public async Task<bool> RemoveCommentLikeAsync(Guid userId, Guid commentId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var rowsDeleted = await _db.CommentLikes
                    .Where(c => c.CommentId == commentId && c.LikedUserId == userId)
                    .ExecuteDeleteAsync();

                if (rowsDeleted == 0)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                await _db.PostComments
                    .Where(c => c.Id == commentId)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.LikeCount, c => c.LikeCount - 1));

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region Helpers

        private static bool IsDuplicateKey(DbUpdateException ex)
        {
            return ex.InnerException is MySqlConnector.MySqlException { Number: 1062 };
        }

        #endregion
    }
}
