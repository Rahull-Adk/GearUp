using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.ServiceDtos.Post;
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

        public async Task<IEnumerable<CommentDto>> GetTopLevelCommentsByPostIdAsync(Guid postId)
        {
            return await _db.PostComments
                .Where(pc => (pc.PostId == postId && pc.ParentCommentId == null))
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    PostId = c.PostId,
                    ParentCommentId = c.ParentCommentId,
                    Content = c.Content,
                    ChildCount = _db.PostComments.Count(pc => pc.ParentCommentId == c.Id),  
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    CommentedUserId = c.CommentedUserId,
                    CommentedUserName = c.CommentedUser!.Username,
                    CommentedUserProfilePictureUrl = c.CommentedUser.AvatarUrl
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<CommentDto>> GetChildCommentsByParentIdAsync(Guid parentCommentId)
        {
            return await _db.PostComments
                .Where(pc => pc.ParentCommentId == parentCommentId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    PostId = c.PostId,
                    ParentCommentId = c.ParentCommentId,
                    ChildCount = _db.PostComments.Count(pc => pc.ParentCommentId == c.Id),
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    CommentedUserId = c.CommentedUserId,
                    CommentedUserName = c.CommentedUser!.Username,
                    CommentedUserProfilePictureUrl = c.CommentedUser.AvatarUrl
                })
                .ToListAsync();
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
