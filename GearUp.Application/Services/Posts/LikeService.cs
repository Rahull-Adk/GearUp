using GearUp.Application.Common;
using GearUp.Application.Interfaces;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.PostServiceInterface;
using GearUp.Domain.Entities.Posts;
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


        public LikeService(ILogger<IPostService> logger,ICommonRepository commonRepository, IPostRepository postRepository, IUserRepository userRepository, ILikeRepository likeRepository, ICommentRepository commentRepository)
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
            _logger.LogInformation("User with id: {UserId} is liking/unliking the Comment with id: {CommentId}", userId,
                commentId);
            string message = "";
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
        { 
            _logger.LogInformation("User with id: {UserId} is liking/unliking the Comment with id: {CommentId}", userId, commentId);
            var userExist = await _userRepository.UserExistAsync(userId);
            if (!userExist)
            {
                _logger.LogWarning("User with Id: {UserId} not found", userId);
                return Result<int>.Failure("User not found", 404);
            }

            var comment = await _commentRepository.GetCommentByIdAsync(commentId);
            var commentExist = await _commentRepository.CommentExistAsync(commentId);

            if (!commentExist)
            {
                _logger.LogWarning("Comment with Id: {CommentId} not found", commentId);
                return Result<int>.Failure("Comment not found", 404);
            }

            var isCommentAlreadyLiked = await _commentRepository.IsCommentAlreadyLikedByUserAsync(commentId, userId);

            string message;
            if (isCommentAlreadyLiked)
            {
                _likeRepository.RemoveCommentLike(userId, commentId);
                message = "Comment unliked successfully";

                _logger.LogInformation("Comment with Id: {CommentId} unliked successfully by user with Id: {UserId}",
                    commentId, userId);
                _logger.LogInformation("Comment with Id: {CommentId} unliked successfully by user with Id: {UserId}", commentId, userId);
            }

            else
            {
                var commentLike = CommentLike.CreateCommentLike(commentId, userId);
                await _likeRepository.AddCommentLikeAsync(commentLike);
                message = "Comment liked successfully";
                _logger.LogInformation("Comment with Id: {CommentId} liked successfully by user with Id: {UserId}",
                    commentId, userId);
                _logger.LogInformation("Comment with Id: {CommentId} liked successfully by user with Id: {UserId}", commentId, userId);
            }

            await _commonRepository.SaveChangesAsync();
            var likeCount = await _commentRepository.GetCommentLikeCountByIdAysnc(commentId);
            await _realTimeNotifier.BroadCastCommentLikes(comment.PostId, commentId, likeCount);
            return Result<int>.Success(likeCount, message, 200);
        }

        public async Task<Result<int>> LikePostAsync(Guid postId, Guid userId)
        {
            _logger.LogInformation("User with Id: {UserId} is liking post with Id: {PostId}", userId, postId);
            string message;
            var postExist = await _postRepository.PostExistAsync(postId);
            if (!postExist)
            {
                _logger.LogWarning("Post with Id: {PostId} not found", postId);
                return Result<int>.Failure("Post not found", 404);
            }

            var userExist = await _userRepository.UserExistAsync(userId);
            if (!userExist)
            {
                _logger.LogWarning("User with Id: {UserId} not found", userId);
                return Result<int>.Failure("User not found", 404);
            }

            var counts = await _postRepository.GetCountsForPostById(postId, userId);


            if (counts.IsLikedByCurrentUser)
            {
                _likeRepository.RemovePostLike(userId, postId);
                message = "Post unliked successfully";

                _logger.LogInformation("Post with Id: {PostId} unliked successfully by user with Id: {UserId}", postId,
                    userId);
                _logger.LogInformation("Post with Id: {PostId} unliked successfully by user with Id: {UserId}", postId, userId);
            }
            else
            {
                var postLike = PostLike.CreateLike(postId, userId);
                await _likeRepository.AddPostLikeAsync(postLike);
                message = "Post liked successfully";
                _logger.LogInformation("Post with Id: {PostId} liked successfully by user with Id: {UserId}", postId,
                    userId);
                _logger.LogInformation("Post with Id: {PostId} liked successfully by user with Id: {UserId}", postId, userId);
            }

            await _commonRepository.SaveChangesAsync();
            var likeCount = await _likeRepository.GetPostLikeCountAsync(postId);
            await _realTimeNotifier.BroadCastPostLikes(postId, likeCount);

            return Result<int>.Success(likeCount, message, 200);
        }
    }
}