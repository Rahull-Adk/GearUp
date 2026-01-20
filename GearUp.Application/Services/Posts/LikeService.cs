using GearUp.Application.Common;
using GearUp.Application.Interfaces;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.PostServiceInterface;
using GearUp.Application.ServiceDtos;
using GearUp.Domain.Entities.Posts;
using GearUp.Domain.Entities.RealTime;
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
        private readonly ICommonRepository _commonRepository;
        private readonly IPostRepository _postRepository;
        private readonly IRealTimeNotifier _realTimeNotifier;


        public LikeService(ILogger<IPostService> logger, ICommonRepository commonRepository,
            IPostRepository postRepository, IUserRepository userRepository, ILikeRepository likeRepository,
            ICommentRepository commentRepository, IRealTimeNotifier realTimeNotifier)
        {
            _logger = logger;
            _commonRepository = commonRepository;
            _realTimeNotifier = realTimeNotifier;
            _postRepository = postRepository;
            _userRepository = userRepository;
            _likeRepository = likeRepository;
            _commentRepository = commentRepository;
        }

        public async Task<Result<int>> LikeCommentAsync(Guid commentId, Guid userId)
        {
            _logger.LogInformation(
                "User {UserId} is liking/unliking Comment {CommentId}",
                userId, commentId);

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

            var alreadyLiked = await _commentRepository
                .IsCommentAlreadyLikedByUserAsync(commentId, userId);

            string message;

            if (alreadyLiked)
            {
                _likeRepository.RemoveCommentLike(userId, commentId);
                message = "Comment unliked successfully";

                _logger.LogInformation(
                    "User {UserId} unliked Comment {CommentId}",
                    userId, commentId);
            }
            else
            {
                var commentLike = CommentLike.CreateCommentLike(commentId, userId);
                await _likeRepository.AddCommentLikeAsync(commentLike);
                message = "Comment liked successfully";

                _logger.LogInformation(
                    "User {UserId} liked Comment {CommentId}",
                    userId, commentId);

                if (comment.CommentedUserId != userId)
                {
                    var actor = await _userRepository.GetUserByIdAsync(userId);
                    var actorName = actor?.Name ?? "Someone";

                    var notification = Notification.CreateNotification(
                        $"{actorName} liked your comment",
                        NotificationEnum.CommentLiked,
                        actorUserId: userId,
                        receiverUserId: comment.CommentedUserId,
                        commentId: commentId,
                        postId: comment.PostId
                    );

                    var notificationDto = new NotificationDto
                    {
                        Title = notification.Title,
                        ActorUserId = notification.ActorUserId,
                        ReceiverUserId = notification.ReceiverUserId,
                        CommentId = notification.CommentId,
                        IsRead = false,
                        NotificationType = notification.NotificationType,
                        SentAt = notification.CreatedAt
                    };
                    await _realTimeNotifier.PushNotification(comment.CommentedUserId, notificationDto);
                }
            }

            await _commonRepository.SaveChangesAsync();
            var likeCount = await _commentRepository.GetCommentLikeCountByIdAysnc(commentId);
            await _realTimeNotifier.BroadCastCommentLikes(comment.PostId, commentId, likeCount);

            return Result<int>.Success(likeCount, message, 200);
        }

        public async Task<Result<int>> LikePostAsync(Guid postId, Guid userId)
        {
            _logger.LogInformation(
                "User with Id: {UserId} is liking/unliking post with Id: {PostId}",
                userId, postId);

            string message;

            var userExist = await _userRepository.UserExistAsync(userId);
            if (!userExist)
            {
                _logger.LogWarning("User with Id: {UserId} not found", userId);
                return Result<int>.Failure("User not found", 404);
            }


            var post = await _postRepository.GetPostEntityByIdAsync(postId);
            if (post is null || (post.Visibility != PostVisibility.Public && post.UserId != userId))
            {
                _logger.LogWarning("Post with Id: {PostId} not found when fetching", postId);
                return Result<int>.Failure("Post not found", 404);
            }

            var counts = await _postRepository.GetCountsForPostById(postId, userId);

            if (counts.IsLikedByCurrentUser)
            {
                _likeRepository.RemovePostLike(userId, postId);
                message = "Post unliked successfully";

                _logger.LogInformation(
                    "Post with Id: {PostId} unliked successfully by user with Id: {UserId}",
                    postId, userId);
            }
            else
            {
                var postLike = PostLike.CreateLike(postId, userId);
                await _likeRepository.AddPostLikeAsync(postLike);
                message = "Post liked successfully";

                _logger.LogInformation(
                    "Post with Id: {PostId} liked successfully by user with Id: {UserId}",
                    postId, userId);

                if (post.UserId != userId)
                {
                    var actor = await _userRepository.GetUserByIdAsync(userId);
                    var actorName = actor?.Name ?? "Someone";

                    var notification = Notification.CreateNotification(
                        $"{actorName} liked your post",
                        NotificationEnum.PostLiked,
                        actorUserId: userId,
                        receiverUserId: post.UserId,
                        postId: postId
                    );


                    var notificationDto = new NotificationDto
                    {
                        Title = notification.Title,
                        ActorUserId = notification.ActorUserId,
                        ReceiverUserId = notification.ReceiverUserId,
                        PostId = notification.PostId,
                        IsRead = false,
                        NotificationType = notification.NotificationType,
                        SentAt = notification.CreatedAt
                    };

                    await _realTimeNotifier.PushNotification(post.UserId, notificationDto);
                }
            }

            await _commonRepository.SaveChangesAsync();

            var likeCount = await _likeRepository.GetPostLikeCountAsync(postId);
            await _realTimeNotifier.BroadCastPostLikes(postId, likeCount);

            return Result<int>.Success(likeCount, message, 200);
        }
    }
}