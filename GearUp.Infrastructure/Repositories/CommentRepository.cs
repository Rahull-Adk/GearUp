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


        public async Task<IEnumerable<CommentDto>> GetTopLevelCommentsByPostIdAsync(Guid postId, Guid userId)
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
                    LikeCount = _db.CommentLikes.Count(pc => pc.CommentId == c.Id),
                    IsLikedByCurrentUser = c.Likes.Any(cl => cl.LikedUserId == userId),
                    ChildCount = _db.PostComments.Count(pc => pc.ParentCommentId == c.Id),
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    CommentedUserId = c.CommentedUserId,
                    CommentedUserName = c.CommentedUser!.Username,
                    CommentedUserProfilePictureUrl = c.CommentedUser.AvatarUrl
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<CommentDto>> GetChildCommentsByParentIdAsync(Guid parentCommentId, Guid userId)
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
                    LikeCount = _db.CommentLikes.Count(pc => pc.CommentId == c.Id),
                    Content = c.Content,
                    IsLikedByCurrentUser = c.Likes.Any(cl => cl.LikedUserId == userId),
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

        public async Task<int> GetCommentLikeCountByIdAsync(Guid commentId)
        {
            return await _db.CommentLikes.CountAsync(cl => cl.CommentId == commentId);
        }
        public async Task<bool> IsCommentAlreadyLikedByUserAsync(Guid commentId, Guid userId)
        {
            return await _db.CommentLikes.AnyAsync(cl => cl.CommentId == commentId && cl.LikedUserId == userId);
        }

        public async Task<bool> CommentExistAsync(Guid commentId)
        {
            return await _db.PostComments.AnyAsync(c => c.Id == commentId);
        }
    }
}
