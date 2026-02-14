using GearUp.Application.Common;
using GearUp.Application.Interfaces;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.PostServiceInterface;
using GearUp.Domain.Entities.Posts;
using GearUp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GearUp.Application.Services.Posts
{
    public class LikeService : ILikeService
    {
        private readonly ILogger<IPostService> _logger;
        private readonly IUserRepository _userRepository;
        private readonly ILikeRepository _likeRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly IPostRepository _postRepository;
        private readonly IRealTimeNotifier _realTimeNotifier;
        private readonly INotificationService _notificationService;

        public LikeService(
            ILogger<IPostService> logger,
            IPostRepository postRepository,
            IUserRepository userRepository,
            ILikeRepository likeRepository,
            ICommentRepository commentRepository,
            IRealTimeNotifier realTimeNotifier,
            INotificationService notificationService)
        {
            _logger = logger;
            _realTimeNotifier = realTimeNotifier;
            _postRepository = postRepository;
            _userRepository = userRepository;
            _likeRepository = likeRepository;
            _commentRepository = commentRepository;
            _notificationService = notificationService;
        }



        public async Task<Result<int>> LikePostAsync(Guid postId, Guid userId)
        {
            _logger.LogInformation("User {UserId} is liking post {PostId}", userId, postId);

            var userExist = await _userRepository.UserExistAsync(userId);
            if (!userExist)
            {
                _logger.LogWarning("User with Id {UserId} not found", userId);
                return Result<int>.Failure("User not found", 404);
            }

            var post = await _postRepository.GetPostEntityByIdAsync(postId);
            if (post is null || (post.Visibility != PostVisibility.Public && post.UserId != userId))
            {
                _logger.LogWarning("Post with Id {PostId} not found", postId);
                return Result<int>.Failure("Post not found", 404);
            }

            var postLike = PostLike.CreateLike(postId, userId);
            var success = await _likeRepository.AddPostLikeAsync(postLike);

            if (!success)
            {
                _logger.LogInformation("Post {PostId} already liked by user {UserId}", postId, userId);
                return Result<int>.Failure("Post already liked", 400);
            }

            _logger.LogInformation("Post {PostId} liked successfully by user {UserId}", postId, userId);

            // Send notification to post owner (persisted + real-time)
            if (post.UserId != userId)
            {
                var actor = await _userRepository.GetUserByIdAsync(userId);
                var actorName = actor?.Name ?? "Someone";

                await _notificationService.CreateAndPushNotificationAsync(
                    "Someone liked your post",
                    $"{actorName} liked your post.",
                    NotificationEnum.PostLiked,
                    actorUserId: userId,
                    receiverUserId: post.UserId,
                    postId: postId
                );
            }

            var counts = await _postRepository.GetCountsForPostById(postId, userId);
            await _realTimeNotifier.BroadCastPostLikes(postId, counts.LikeCount);

            return Result<int>.Success(counts.LikeCount, "Post liked successfully", 200);
        }

        public async Task<Result<int>> UnlikePostAsync(Guid postId, Guid userId)
        {
            _logger.LogInformation("User {UserId} is unliking post {PostId}", userId, postId);

            var userExist = await _userRepository.UserExistAsync(userId);
            if (!userExist)
            {
                _logger.LogWarning("User with Id {UserId} not found", userId);
                return Result<int>.Failure("User not found", 404);
            }

            var post = await _postRepository.GetPostEntityByIdAsync(postId);
            if (post is null || (post.Visibility != PostVisibility.Public && post.UserId != userId))
            {
                _logger.LogWarning("Post with Id {PostId} not found", postId);
                return Result<int>.Failure("Post not found", 404);
            }

            var success = await _likeRepository.RemovePostLikeAsync(userId, postId);

            if (!success)
            {
                _logger.LogInformation("Post {PostId} was not liked by user {UserId}", postId, userId);
                return Result<int>.Failure("Post was not liked", 400);
            }

            _logger.LogInformation("Post {PostId} unliked successfully by user {UserId}", postId, userId);

            var counts = await _postRepository.GetCountsForPostById(postId, userId);
            await _realTimeNotifier.BroadCastPostLikes(postId, counts.LikeCount);

            return Result<int>.Success(counts.LikeCount, "Post unliked successfully", 200);
        }



        public async Task<Result<int>> LikeCommentAsync(Guid commentId, Guid userId)
        {
            _logger.LogInformation("User {UserId} is liking comment {CommentId}", userId, commentId);

            var userExist = await _userRepository.UserExistAsync(userId);
            if (!userExist)
            {
                _logger.LogWarning("User with Id {UserId} not found", userId);
                return Result<int>.Failure("User not found", 404);
            }

            var comment = await _commentRepository.GetCommentByIdAsync(commentId);
            if (comment is null)
            {
                _logger.LogWarning("Comment with Id {CommentId} not found", commentId);
                return Result<int>.Failure("Comment not found", 404);
            }

            var commentLike = CommentLike.CreateCommentLike(commentId, userId);
            var success = await _likeRepository.AddCommentLikeAsync(commentLike);

            if (!success)
            {
                _logger.LogInformation("Comment {CommentId} already liked by user {UserId}", commentId, userId);
                return Result<int>.Failure("Comment already liked", 400);
            }

            _logger.LogInformation("Comment {CommentId} liked successfully by user {UserId}", commentId, userId);

            // Send notification to comment owner (persisted + real-time)
            if (comment.CommentedUserId != userId)
            {
                var actor = await _userRepository.GetUserByIdAsync(userId);
                var actorName = actor?.Name ?? "Someone";

                await _notificationService.CreateAndPushNotificationAsync(
                    "Someone liked your comment",
                    $"{actorName} liked your comment.",
                    NotificationEnum.CommentLiked,
                    actorUserId: userId,
                    receiverUserId: comment.CommentedUserId,
                    commentId: commentId,
                    postId: comment.PostId
                );
            }

            // Get updated count from database
            var updatedComment = await _commentRepository.GetCommentByIdAsync(commentId);
            var likeCount = updatedComment?.LikeCount ?? 0;

            await _realTimeNotifier.BroadCastCommentLikes(comment.PostId, commentId, likeCount);

            return Result<int>.Success(likeCount, "Comment liked successfully", 200);
        }

        public async Task<Result<int>> UnlikeCommentAsync(Guid commentId, Guid userId)
        {
            _logger.LogInformation("User {UserId} is unliking comment {CommentId}", userId, commentId);

            var userExist = await _userRepository.UserExistAsync(userId);
            if (!userExist)
            {
                _logger.LogWarning("User with Id {UserId} not found", userId);
                return Result<int>.Failure("User not found", 404);
            }

            var comment = await _commentRepository.GetCommentByIdAsync(commentId);
            if (comment is null)
            {
                _logger.LogWarning("Comment with Id {CommentId} not found", commentId);
                return Result<int>.Failure("Comment not found", 404);
            }

            var success = await _likeRepository.RemoveCommentLikeAsync(userId, commentId);

            if (!success)
            {
                _logger.LogInformation("Comment {CommentId} was not liked by user {UserId}", commentId, userId);
                return Result<int>.Failure("Comment was not liked", 400);
            }

            _logger.LogInformation("Comment {CommentId} unliked successfully by user {UserId}", commentId, userId);

            // Get updated count from database
            var updatedComment = await _commentRepository.GetCommentByIdAsync(commentId);
            var likeCount = updatedComment?.LikeCount ?? 0;

            await _realTimeNotifier.BroadCastCommentLikes(comment.PostId, commentId, likeCount);

            return Result<int>.Success(likeCount, "Comment unliked successfully", 200);
        }


    }
}
