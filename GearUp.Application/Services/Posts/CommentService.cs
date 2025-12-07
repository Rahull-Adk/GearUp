using System.Linq;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.PostServiceInterface;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Domain.Entities.Posts;
using Microsoft.Extensions.Logging;

namespace GearUp.Application.Services.Posts
{
    public class CommentService : ICommentService
    {
        private readonly ILogger<ICommentService> _logger;
        private readonly ICacheService _cache;
        private readonly IUserRepository _userRepository;
        private readonly ICommonRepository _commonRepository;
        private readonly IPostRepository _postRepository;
        private readonly ICommentRepository _commentRepository;

        public CommentService(ILogger<ICommentService> logger, ICacheService cache, ICommonRepository commonRepository,  IPostRepository postRepository, IUserRepository userRepository, ICommentRepository commentRepository)
        {
            _logger = logger;
            _cache = cache;
            _commonRepository = commonRepository;
            _postRepository = postRepository;
            _userRepository = userRepository;
            _commentRepository = commentRepository;
        }

        public async Task<Result<CommentDto>> PostCommentAsync(CreateCommentDto comment, Guid userId)
        {
            var cacheKey = $"post:{comment.PostId}";
            _logger.LogInformation("User with Id: {UserId} is commenting on post with Id: {PostId}", userId, comment.PostId);

            var post = await _postRepository.GetPostByIdAsync(comment.PostId);
            if (post == null)
            {
                _logger.LogWarning("Post with Id: {PostId} not found", comment.PostId);
                return Result<CommentDto>.Failure("Post not found", 404);
            }

            if(string.IsNullOrEmpty(comment.Text))
            {
                _logger.LogWarning("Comment length is invalid. UserId: {UserId}, PostId: {PostId}", userId, comment.PostId);
                return Result<CommentDto>.Failure("Invalid comment length", 400);
            }

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User with Id: {UserId} not found", userId);
                return Result<CommentDto>.Failure("User not found", 404);
            }

            if (comment.ParentCommentId.HasValue)
            {
                var parentComment = await _commentRepository.GetCommentByIdAsync(comment.ParentCommentId.Value);
                if (parentComment == null || parentComment.PostId != comment.PostId)
                {
                    _logger.LogWarning("Parent comment with Id: {ParentCommentId} not found for PostId: {PostId}", comment.ParentCommentId, comment.PostId);
                    return Result<CommentDto>.Failure("Parent comment not found for this post", 404);
                }
            }



            var postComment = PostComment.CreateComment(comment.PostId, userId, comment.Text, comment.ParentCommentId);
            await _commentRepository.AddCommentAsync(postComment);
            await _commonRepository.SaveChangesAsync();

            await _cache.RemoveAsync(cacheKey);
            var commentDto = new CommentDto
            {
                Id = postComment.Id,
                PostId = postComment.PostId,
                CommentedUserId = postComment.CommentedUserId,
                Content = postComment.Content,
                ParentCommentId = postComment.ParentCommentId,
                CreatedAt = postComment.CreatedAt,
                UpdatedAt = postComment.UpdatedAt,
                CommentedUserName = user.Name,
                CommentedUserProfilePictureUrl = user.AvatarUrl,
                LikeCount = 0,
                IsLikedByCurrentUser = false,
                Replies = []
            };
            _logger.LogInformation("User with Id: {UserId} commented successfully on post with Id: {PostId}", userId, comment.PostId);

            return Result<CommentDto>.Success(commentDto, "Comment added successfully", 201);

        }

        public async Task<Result<bool>> DeleteCommentAsync(Guid commentId, Guid userId)
        {
            _logger.LogInformation("User with Id: {UserId} is attempting to delete comment with Id: {CommentId}", userId, commentId);

            var commentInfo = await _commentRepository.GetCommentByIdAsync(commentId);
            if (commentInfo == null)
            {
                _logger.LogWarning("Comment with Id: {CommentId} not found", commentId);
                return Result<bool>.Failure("Comment not found", 404);
            }
        var commentEntity = await _commentRepository.GetCommentByIdAsync(commentId);

            if (commentEntity == null)
            {
                _logger.LogWarning("Comment entity with Id: {CommentId} not found", commentId);
                return Result<bool>.Failure("Comment not found", 404);
            }

            if (commentEntity.CommentedUserId != userId)
            {
                _logger.LogWarning("User with Id: {UserId} is not authorized to delete comment with Id: {CommentId}", userId, commentId);
                return Result<bool>.Failure("You are not authorized to delete this comment", 403);
            }

            commentEntity.DeleteComment();
            await _commonRepository.SaveChangesAsync();

            await _cache.RemoveAsync($"post:{commentInfo.PostId}");

            _logger.LogInformation("User with Id: {UserId} deleted comment with Id: {CommentId} successfully", userId, commentId);
            return Result<bool>.Success(true, "Comment deleted successfully", 200);

        }


        public async Task<Result<CommentDto>> UpdateCommentAsync(Guid commentId, Guid userId, string updatedContent)
        {
            _logger.LogInformation("User with Id: {UserId} is attempting to update comment with Id: {CommentId}", userId, commentId);

            if (string.IsNullOrWhiteSpace(updatedContent))
            {
                _logger.LogWarning("Updated content is invalid. UserId: {UserId}, CommentId: {CommentId}", userId, commentId);
                return Result<CommentDto>.Failure("Invalid comment content", 400);
            }

            var commentInfo = await _commentRepository.GetCommentByIdAsync(commentId);
            if (commentInfo == null)
            {
                _logger.LogWarning("Comment with Id: {CommentId} not found", commentId);
                return Result<CommentDto>.Failure("Comment not found", 404);
            }
            
            var commentEntity = await _commentRepository.GetCommentByIdAsync(commentId);
            if (commentEntity == null)
            {
                _logger.LogWarning("Comment entity with Id: {CommentId} not found", commentId);
                return Result<CommentDto>.Failure("Comment not found", 404);
            }

            if (commentEntity.CommentedUserId != userId)
            {
                _logger.LogWarning("User with Id: {UserId} is not authorized to update comment with Id: {CommentId}", userId, commentId);
                return Result<CommentDto>.Failure("You are not authorized to update this comment", 403);
            }

            commentEntity.UpdateContent(updatedContent);
            await _commonRepository.SaveChangesAsync();



            var updatedDto = new CommentDto
            {
                Id = commentEntity.Id,
                PostId = commentEntity.PostId,
                CommentedUserId = commentEntity.CommentedUserId,
                Content = updatedContent,
                ParentCommentId = commentEntity.ParentCommentId,
                CreatedAt = commentEntity.CreatedAt,
                UpdatedAt = commentEntity.UpdatedAt,
                IsEdited = commentEntity.UpdatedAt > commentEntity.CreatedAt,
            };

        
            await _cache.RemoveAsync($"post:{commentInfo.PostId}");

            _logger.LogInformation("User with Id: {UserId} updated comment with Id: {CommentId} successfully", userId, commentId);
            return Result<CommentDto>.Success(updatedDto, "Comment updated successfully", 200);
        }
    }
}
