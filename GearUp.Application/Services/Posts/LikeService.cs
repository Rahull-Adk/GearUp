using GearUp.Application.Common;
using GearUp.Application.Interfaces;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
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


        public LikeService(ILogger<IPostService> logger, ICacheService cache, ICommonRepository commonRepository, IPostRepository postRepository, IUserRepository userRepository, IRealTimeNotifier realTimeNotifier, ILikeRepository likeRepository, ICommentRepository commentRepository)
        {
            _logger = logger;
            _commonRepository = commonRepository;
            _postRepository = postRepository;
            _userRepository = userRepository;
            _likeRepository = likeRepository;
            _commentRepository = commentRepository;
        }
        public async Task<Result<int>> LikeCommentAsync(Guid commentId, Guid userId)
        {

            _logger.LogInformation("User with id: {UserId} is liking/unliking the Comment with id: {CommentId}", userId, commentId);
            var message = string.Empty;
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User with Id: {UserId} not found", userId);
                return Result<int>.Failure("User not found", 404);
            }
            var comment = await _commentRepository.GetCommentByIdAsync(commentId);

            if (comment == null)
            {
                _logger.LogWarning("Comment with Id: {CommentId} not found", commentId);
                return Result<int>.Failure("Comment not found", 404);
            }
            var isCommentAlreadyLiked = await _commentRepository.IsCommentAlreadyLikedByUserAsync(commentId, userId);

            bool isNowLiked;
            if (isCommentAlreadyLiked)
            {
                await _likeRepository.RemoveCommentLikeAsync(userId, commentId);
                message = "Comment unliked successfully";
                isNowLiked = false;
                _logger.LogInformation("Comment with Id: {CommentId} unliked successfully by user with Id: {UserId}", commentId, userId);
            }

            else
            {
                var commentLike = CommentLike.CreateCommentLike(commentId, userId);
                await _likeRepository.AddCommentLikeAsync(commentLike);
                message = "Comment liked successfully";
                isNowLiked = true;
                _logger.LogInformation("Comment with Id: {CommentId} liked successfully by user with Id: {UserId}", commentId, userId);
            }

            await _commonRepository.SaveChangesAsync();

            var likeCount = await _commentRepository.GetCommentLikeCountByIdAysnc(commentId);
            return Result<int>.Success(likeCount, message, 200);


        }

        public async Task<Result<int>> LikePostAsync(Guid postId, Guid userId)
        {
            _logger.LogInformation("User with Id: {UserId} is liking post with Id: {PostId}", userId, postId);
            string message;
            var post = await _postRepository.GetPostByIdAsync(postId, userId);
            if (post == null)
            {
                _logger.LogWarning("Post with Id: {PostId} not found", postId);
                return Result<int>.Failure("Post not found", 404);
            }

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User with Id: {UserId} not found", userId);
                return Result<int>.Failure("User not found", 404);
            }
            var counts = await _postRepository.GetCountsForPostById(postId, userId);

            bool isNowLiked;
            if (counts.IsLikedByCurrentUser)
            {
                await _likeRepository.RemovePostLikeAsync(userId, postId);
                message = "Post unliked successfully";
                isNowLiked = false;
                _logger.LogInformation("Post with Id: {PostId} unliked successfully by user with Id: {UserId}", postId, userId);
            }
            else
            {
                var postLike = PostLike.CreateLike(postId, userId);
                await _likeRepository.AddPostLikeAsync(postLike);
                message = "Post liked successfully";
                isNowLiked = true;
                _logger.LogInformation("Post with Id: {PostId} liked successfully by user with Id: {UserId}", postId, userId);
            }

            await _commonRepository.SaveChangesAsync();

            var likeCount = await _likeRepository.GetPostLikeCountAsync(postId);

            return Result<int>.Success(likeCount, message, 200);
        }

    }
}
