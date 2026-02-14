using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.Interfaces;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.PostServiceInterface;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Application.ServiceDtos.Socials;
using GearUp.Domain.Entities.Posts;
using GearUp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GearUp.Application.Services.Posts
{
    public class CommentService : ICommentService
    {
        private readonly ILogger<ICommentService> _logger;
        private readonly IUserRepository _userRepository;
        private readonly ICommonRepository _commonRepository;
        private readonly IPostRepository _postRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly IRealTimeNotifier _realTimeNotifier;
        private readonly INotificationService _notificationService;

        public CommentService(
            ILogger<ICommentService> logger,
            ICommonRepository commonRepository,
            IPostRepository postRepository,
            IUserRepository userRepository,
            ICommentRepository commentRepository,
            IRealTimeNotifier realTimeNotifier,
            INotificationService notificationService)
        {
            _logger = logger;
            _commonRepository = commonRepository;
            _postRepository = postRepository;
            _userRepository = userRepository;
            _commentRepository = commentRepository;
            _realTimeNotifier = realTimeNotifier;
            _notificationService = notificationService;
        }

        public async Task<Result<CommentDto>> PostCommentAsync(CreateCommentDto comment, Guid userId)
        {
            _logger.LogInformation(
                "User with Id: {UserId} is commenting on post with Id: {PostId}",
                userId, comment.PostId);

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user is null)
            {
                _logger.LogWarning("User with Id: {UserId} not found", userId);
                return Result<CommentDto>.Failure("User not found", 404);
            }

            var post = await _postRepository.GetPostEntityByIdAsync(comment.PostId);
            if (post is null)
            {
                _logger.LogWarning("Post with Id: {PostId} not found", comment.PostId);
                return Result<CommentDto>.Failure("Post not found", 404);
            }

            if (string.IsNullOrWhiteSpace(comment.Content))
            {
                _logger.LogWarning(
                    "Comment length is invalid. UserId: {UserId}, PostId: {PostId}",
                    userId, comment.PostId);
                return Result<CommentDto>.Failure("Invalid comment length", 400);
            }

            PostComment? parentComment = null;

            if (comment.ParentCommentId.HasValue)
            {
                parentComment = await _commentRepository.GetCommentByIdAsync(comment.ParentCommentId.Value);
                if (parentComment == null || parentComment.PostId != comment.PostId)
                {
                    _logger.LogWarning(
                        "Parent comment with Id: {ParentCommentId} not found for PostId: {PostId}",
                        comment.ParentCommentId, comment.PostId);
                    return Result<CommentDto>.Failure("Parent comment not found for this post", 404);
                }
            }

            var postComment = PostComment.CreateComment(comment.PostId, userId, comment.Content, comment.ParentCommentId);
            await _commentRepository.AddCommentAsync(postComment);

            // Increment counts transactionally
            post.IncrementCommentCount();
            if (parentComment != null)
            {
                parentComment.IncrementReplyCount();
            }

            var receiverUserId = parentComment?.CommentedUserId ?? post.UserId;

            await _commonRepository.SaveChangesAsync();

            var commentDto = new CommentDto
            {
                ParentCommentId = comment.ParentCommentId,
                PostId = comment.PostId,
                CommentedUserId = userId,
                Content = comment.Content,
                CreatedAt = postComment.CreatedAt,
                Id = postComment.Id,
                CommentedUserName = user.Username,
                CommentedUserProfilePictureUrl = user.AvatarUrl,
                LikeCount = 0
            };

            await _realTimeNotifier.BroadCastComments(comment.PostId, commentDto);

            // Send notification (persisted + real-time)
            if (receiverUserId != userId)
            {
                var notificationType = parentComment is not null
                    ? NotificationEnum.CommentReplied
                    : NotificationEnum.PostCommented;

                var title = parentComment is not null
                    ? "New reply on your comment"
                    : "New comment on your post";

                var content = parentComment is not null
                    ? $"{user.Name} replied to your comment: \"{comment.Content}\""
                    : $"{user.Name} commented on your post: \"{comment.Content}\"";

                await _notificationService.CreateAndPushNotificationAsync(
                    title,
                    content,
                    notificationType,
                    actorUserId: userId,
                    receiverUserId: receiverUserId,
                    postId: post.Id,
                    commentId: postComment.Id
                );
            }

            _logger.LogInformation(
                "User with Id: {UserId} commented successfully on post with Id: {PostId}",
                userId, comment.PostId);
            return Result<CommentDto>.Success(commentDto, "Comment added successfully", 201);
        }


        public async Task<Result<bool>> DeleteCommentAsync(Guid commentId, Guid userId)
        {
            _logger.LogInformation("User with Id: {UserId} is attempting to delete comment with Id: {CommentId}",
                userId, commentId);

            var commentEntity = await _commentRepository.GetCommentByIdAsync(commentId);

            if (commentEntity == null)
            {
                _logger.LogWarning("Comment entity with Id: {CommentId} not found", commentId);
                return Result<bool>.Failure("Comment not found", 404);
            }

            if (commentEntity.CommentedUserId != userId)
            {
                _logger.LogWarning("User with Id: {UserId} is not authorized to delete comment with Id: {CommentId}",
                    userId, commentId);
                return Result<bool>.Failure("You are not authorized to delete this comment", 403);
            }

            // Get parent comment and post to decrement counts
            var post = await _postRepository.GetPostEntityByIdAsync(commentEntity.PostId);
            if (post != null)
            {
                post.DecrementCommentCount();
            }

            if (commentEntity.ParentCommentId.HasValue)
            {
                var parentComment = await _commentRepository.GetCommentByIdAsync(commentEntity.ParentCommentId.Value);
                if (parentComment != null)
                {
                    parentComment.DecrementReplyCount();
                }
            }

            commentEntity.DeleteComment();
            await _commonRepository.SaveChangesAsync();
            _logger.LogInformation("User with Id: {UserId} deleted comment with Id: {CommentId} successfully", userId,
                commentId);
            return Result<bool>.Success(true, "Comment deleted successfully", 200);
        }

        public async Task<Result<CommentDto>> UpdateCommentAsync(Guid commentId, Guid userId, string updatedContent)
        {
            _logger.LogInformation("User with Id: {UserId} is attempting to update comment with Id: {CommentId}",
                userId, commentId);

            if (string.IsNullOrWhiteSpace(updatedContent))
            {
                _logger.LogWarning("Updated content is invalid. UserId: {UserId}, CommentId: {CommentId}", userId,
                    commentId);
                return Result<CommentDto>.Failure("Invalid comment content", 400);
            }

            var commentEntity = await _commentRepository.GetCommentByIdAsync(commentId);
            if (commentEntity == null)
            {
                _logger.LogWarning("Comment entity with Id: {CommentId} not found", commentId);
                return Result<CommentDto>.Failure("Comment not found", 404);
            }

            if (commentEntity.CommentedUserId != userId)
            {
                _logger.LogWarning("User with Id: {UserId} is not authorized to update comment with Id: {CommentId}",
                    userId, commentId);
                return Result<CommentDto>.Failure("You are not authorized to update this comment", 403);
            }

            commentEntity.UpdateContent(updatedContent);
            await _commonRepository.SaveChangesAsync();
            _logger.LogInformation("User with Id: {UserId} updated comment with Id: {CommentId} successfully", userId,
                commentId);
            return Result<CommentDto>.Success(null, "Comment updated successfully", 200);
        }

        public async Task<Result<IEnumerable<CommentDto>>> GetParentCommentsByPostId(Guid postId, Guid userId)
        {
            _logger.LogInformation("Fetching parent comments for post with Id: {PostId}", postId);
            var postExist = await _postRepository.PostExistAsync(postId);
            if (!postExist)
            {
                _logger.LogWarning("Post with Id: {PostId} not found", postId);
                return Result<IEnumerable<CommentDto>>.Failure("Post not found", 404);
            }

            var comments = await _commentRepository.GetTopLevelCommentsByPostIdAsync(postId, userId);
            IEnumerable<CommentDto> commentDtos = comments as CommentDto[] ?? comments.ToArray();
            _logger.LogInformation("Fetched {CommentCount} comments for post with Id: {PostId}", commentDtos.Count(),
                postId);

            return Result<IEnumerable<CommentDto>>.Success(commentDtos, "Comments fetched successfully", 200);
        }

        public async Task<Result<IEnumerable<CommentDto>>> GetChildCommentsByParentId(Guid parentCommentId, Guid userId)
        {
            _logger.LogInformation("Fetching child comments for parent comment with Id: {ParentCommentId}",
                parentCommentId);
            var parentCommentExist = await _commentRepository.CommentExistAsync(parentCommentId);
            if (!parentCommentExist)
            {
                _logger.LogWarning("Parent comment with Id: {ParentCommentId} not found", parentCommentId);
                return Result<IEnumerable<CommentDto>>.Failure("Parent comment not found", 404);
            }

            var comments = await _commentRepository.GetChildCommentsByParentIdAsync(parentCommentId, userId);
            _logger.LogInformation(
                "Fetched {CommentCount} child comments for parent comment with Id: {ParentCommentId}", comments.Count(),
                parentCommentId);
            return Result<IEnumerable<CommentDto>>.Success(comments, "Child comments fetched successfully", 200);
        }

        public async Task<Result<CursorPageResult<UserEngagementDto>>> GetCommentLikersAsync(Guid commentId, string? cursorString)
        {
            _logger.LogInformation("Fetching likers for comment with Id: {CommentId}", commentId);

            var commentExist = await _commentRepository.CommentExistAsync(commentId);
            if (!commentExist)
            {
                _logger.LogWarning("Comment with Id: {CommentId} not found", commentId);
                return Result<CursorPageResult<UserEngagementDto>>.Failure("Comment not found", 404);
            }

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    return Result<CursorPageResult<UserEngagementDto>>.Failure("Invalid cursor", 400);
                }
            }

            var likers = await _commentRepository.GetCommentLikersAsync(commentId, cursor);

            _logger.LogInformation("Fetched {LikerCount} likers for comment with Id: {CommentId}",
                likers.Items.Count(), commentId);

            return Result<CursorPageResult<UserEngagementDto>>.Success(likers, "Comment likers fetched successfully");
        }
    }
}