using System;
using System.Collections.Generic;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Domain.Entities.Posts;
using GearUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GearUp.Infrastructure.Repositories
{
    public class CommentRepository : ICommentRepository
    {
            private readonly GearUpDbContext _db;
        public CommentRepository(GearUpDbContext db)
        {
            _db = db;
        }

        public async Task AddCommentAsync(PostComment comment)
        {
            await _db.PostComments.AddAsync(comment);
        }

        public async Task<Dictionary<Guid, int>> GetCommentsLikeCount(List<Guid> commentIds)
        {
            return await _db.PostComments.Where(pc => commentIds.Contains(pc.Id))
                .Select(pc => new
                {
                    pc.Id,
                    LikeCount = _db.CommentLikes.Count(pcl => pcl.CommentId == pc.Id)
                })
                .ToDictionaryAsync(k => k.Id, v => v.LikeCount);
        }

        public async Task<List<PostComment>> GetAllCommentsByPostIdAsync(Guid postId)
        {
            return await _db.PostComments
                .Where(pc => pc.PostId == postId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Guid>>? GetAllCommentsLikedByUser(Guid userId, List<Guid> commentIds)
        {
            return await _db.CommentLikes.Where(cl => cl.LikedUserId == userId && commentIds.Contains(cl.CommentId))
                .Select(cl => cl.CommentId)
                .ToListAsync();
        }

        public async Task<int> GetPostCommentCountAsync(Guid postId)
        {
            return await _db.PostComments.CountAsync(pc => pc.PostId == postId);
        }

        public async Task<PostComment?> GetCommentByIdAsync(Guid commentId)
        {
            return await _db.PostComments.Where(c => c.Id == commentId)
          .FirstOrDefaultAsync();
        }

        public async Task<int> GetCommentLikeCountByIdAysnc(Guid commentId)
        {
            return await _db.CommentLikes.CountAsync(cl => cl.CommentId == commentId);
        }
        public async Task<bool> IsCommentAlreadyLikedByUserAsync(Guid commentId, Guid userId)
        {
            return await _db.CommentLikes.AnyAsync(cl => cl.CommentId == commentId && cl.LikedUserId == userId);
        }
    }
}
