using System.Runtime.InteropServices;
using System.Collections.Generic;
using GearUp.Application.Common;
using GearUp.Application.Interfaces;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.PostServiceInterface;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Domain.Entities.Posts;
using Microsoft.Extensions.Logging;

namespace GearUp.Application.Services.Posts
{
    public class LikeService : ILikeService
    {
        private readonly ILogger<IPostService> _logger;
        private readonly ICacheService _cache;
        private readonly IUserRepository _userRepository;
        private readonly ILikeRepository _likeRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly ICommonRepository _commonRepository;
        private readonly IPostRepository _postRepository;
        private readonly IRealTimeNotifier _realTimeNotifier;


        public LikeService(ILogger<IPostService> logger, ICacheService cache, ICommonRepository commonRepository, IPostRepository postRepository, IUserRepository userRepository, IRealTimeNotifier realTimeNotifier, ILikeRepository likeRepository, ICommentRepository commentRepository)
        {
            _logger = logger;
            _cache = cache;
            _commonRepository = commonRepository;
            _postRepository = postRepository;
            _userRepository = userRepository;
            _realTimeNotifier = realTimeNotifier;
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

            var cacheKey = $"post:{comment.PostId}";

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

            // Update per-post cache item if present
            var cachedPost = await _cache.GetAsync<PostResponseDto>($"post:{comment.PostId}");
            if (cachedPost != null)
            {
                if (UpdateCommentInPostDto(cachedPost, commentId, likeCount, isNowLiked))
                {
                    await _cache.SetAsync($"post:{comment.PostId}", cachedPost, TimeSpan.FromMinutes(15));
                }
            }

            // Update cached pages entries (try to patch instead of invalidating all)
            var pagesIndexKey = "posts:all:pages";
            var pages = await _cache.GetAsync<List<string>>(pagesIndexKey);
            if (pages != null)
            {
                foreach (var pk in pages)
                {
                    var page = await _cache.GetAsync<PageResult<PostResponseDto>>(pk);
                    if (page == null) continue;
                    var modified = false;
                    foreach (var post in page.Items)
                    {
                        if (post.Id == comment.PostId)
                        {
                            if (UpdateCommentInPostDto(post, commentId, likeCount, isNowLiked)) modified = true;
                        }
                    }
                    if (modified)
                    {
                        await _cache.SetAsync(pk, page, TimeSpan.FromMinutes(15));
                    }
                }
            }

            // also remove single post cache to be safe for viewers relying on aggregated counts elsewhere
            await _cache.RemoveAsync(cacheKey);

            await _realTimeNotifier.BroadCastLikesToCommentViewers(commentId, likeCount);
            return Result<int>.Success(likeCount, message, 200);


        }

        public async Task<Result<int>> LikePostAsync(Guid postId, Guid userId)
        {
            var cacheKey = $"post:{postId}";
            _logger.LogInformation("User with Id: {UserId} is liking post with Id: {PostId}", userId, postId);
            string message;
            var post = await _postRepository.GetPostByIdAsync(postId);
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

            // Update single post cache if present
            var cachedPost = await _cache.GetAsync<PostResponseDto>(cacheKey);
            if (cachedPost != null)
            {
                cachedPost.LikeCount = likeCount;
                cachedPost.IsLikedByCurrentUser = isNowLiked;
                await _cache.SetAsync(cacheKey, cachedPost, TimeSpan.FromMinutes(15));
            }

            // Update cached pages entries to reflect new like state
            var pagesIndexKey2 = "posts:all:pages";
            var pages2 = await _cache.GetAsync<List<string>>(pagesIndexKey2);
            if (pages2 != null)
            {
                foreach (var pk in pages2)
                {
                    var page = await _cache.GetAsync<PageResult<PostResponseDto>>(pk);
                    if (page == null) continue;
                    var modified = false;
                    foreach (var p in page.Items)
                    {
                        if (p.Id == postId)
                        {
                            p.LikeCount = likeCount;
                            p.IsLikedByCurrentUser = isNowLiked;
                            modified = true;
                        }
                    }
                    if (modified)
                    {
                        await _cache.SetAsync(pk, page, TimeSpan.FromMinutes(15));
                    }
                }
            }

            await _realTimeNotifier.BroadCastLikeToPostViewers(postId, likeCount);

            return Result<int>.Success(likeCount, message, 200);
        }

        private bool UpdateCommentInPostDto(PostResponseDto postDto, Guid commentId, int likeCount, bool isLiked)
        {
            bool modified = false;
            if (postDto.LatestComments == null) return false;

            foreach (var comment in postDto.LatestComments)
            {
                if (UpdateCommentRecursive(comment, commentId, likeCount, isLiked)) modified = true;
            }
            return modified;
        }

        private bool UpdateCommentRecursive(GearUp.Application.ServiceDtos.Post.CommentDto comment, Guid commentId, int likeCount, bool isLiked)
        {
            if (comment.Id == commentId)
            {
                comment.LikeCount = likeCount;
                comment.IsLikedByCurrentUser = isLiked;
                return true;
            }
            if (comment.Replies == null) return false;
            foreach (var r in comment.Replies)
            {
                if (UpdateCommentRecursive(r, commentId, likeCount, isLiked)) return true;
            }
            return false;
        }
    }
}
