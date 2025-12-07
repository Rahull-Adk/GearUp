using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public async Task AddPostLikeAsync(PostLike pl)
        {
            await _db.PostLikes.AddAsync(pl);
        }
        public async Task RemovePostLikeAsync(Guid userId, Guid postId)
        {
            _db.PostLikes.RemoveRange(_db.PostLikes.Where(p => p.PostId == postId && p.LikedUserId == userId));
        }
        public async Task<int> GetPostLikeCountAsync(Guid postId)
        {
            return await _db.PostLikes.AsNoTracking().CountAsync(pl => pl.PostId == postId);
        }
        public async Task AddCommentLikeAsync(CommentLike cl)
        {
            await _db.CommentLikes.AddAsync(cl);
        }
        public async Task RemoveCommentLikeAsync(Guid userId, Guid commentId)
        {
            _db.CommentLikes.RemoveRange(_db.CommentLikes.Where(c => c.CommentId == commentId && c.LikedUserId == userId));
        }


    }
}
