using FluentValidation;
using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.PostServiceInterface;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Application.ServiceDtos.Socials;
using GearUp.Domain.Entities.Posts;
using GearUp.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace GearUp.Application.Services.Posts
{
    public class PostService : IPostService
    {
        private readonly ILogger<IPostService> _logger;
        private readonly IValidator<CreatePostRequestDto> _createPostValidator;
        private readonly IUserRepository _userRepository;
        private readonly ICommonRepository _commonRepository;
        private readonly ICarRepository _carRepository;
        private readonly IPostRepository _postRepository;
        private readonly IViewRepository _viewRepository;
        private readonly ICacheService _cacheService;

        private const string FeedVersionScope = "posts:feed:version";
        private static readonly TimeSpan FeedCacheTtl = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan FeedVersionTtl = TimeSpan.FromMinutes(10);

        public PostService(ILogger<IPostService> logger, IValidator<CreatePostRequestDto> createPostValidator,
            ICommonRepository commonRepository, ICarRepository carRepository, IPostRepository postRepository,
            IUserRepository userRepository, IViewRepository viewRepository, ICacheService cacheService)
        {
            _logger = logger;
            _createPostValidator = createPostValidator;
            _commonRepository = commonRepository;
            _carRepository = carRepository;
            _postRepository = postRepository;
            _userRepository = userRepository;
            _viewRepository = viewRepository;
            _cacheService = cacheService;
        }

        public async Task<Result<CursorPageResult<PostListResponseDto>>> GetLatestFeedAsync(Guid userId, string cursor, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching page posts for user: {UserId}", userId);
            Cursor? c = null;
            if (!string.IsNullOrEmpty(cursor) && !Cursor.TryDecode(cursor, out c))
            {
                throw new Domain.Exceptions.ValidationException("Invalid cursor");
            }

            var cacheKey = await BuildFeedCacheKeyAsync("feed", userId, cursor);
            var cachedPage = await _cacheService.GetAsync<CursorPageResult<Guid>>(cacheKey);
            
            if (cachedPage != null)
            {
                var posts = new List<PostListResponseDto>();
                bool allFound = true;

                foreach (var id in cachedPage.Items)
                {
                    var post = await _cacheService.GetHashAsync<PostListResponseDto>($"posts:details:{id}");
                    if (post != null)
                    {
                        posts.Add(post);
                    }
                    else
                    {
                        allFound = false;
                        break;
                    }
                }

                if (allFound)
                {
                    _logger.LogInformation("Feed IDs and details fetched from cache");
                    return Result<CursorPageResult<PostListResponseDto>>.Success(new CursorPageResult<PostListResponseDto>
                    {
                        Items = posts,
                        NextCursor = cachedPage.NextCursor,
                        HasMore = cachedPage.HasMore
                    }, "Feed fetched from cache");
                }
            }

            var pageResult = await _postRepository.GetLatestFeedAsync(c, userId, cancellationToken);
            
            // Cache individual posts
            foreach (var post in pageResult.Items)
            {
                await _cacheService.SetHashAsync($"posts:details:{post.Id}", post, FeedCacheTtl);
            }

            // Cache the list of IDs
            var idPage = new CursorPageResult<Guid>
            {
                Items = pageResult.Items.Select(p => p.Id).ToList(),
                NextCursor = pageResult.NextCursor,
                HasMore = pageResult.HasMore
            };
            await _cacheService.SetAsync(cacheKey, idPage, FeedCacheTtl);

            _logger.LogInformation("Successfully fetched {PostCount} posts for feed", pageResult.Items.Count());
            return Result<CursorPageResult<PostListResponseDto>>.Success(pageResult, "Feed fetched successfully", 200);
        }

        public async Task<Result<CursorPageResult<PostListResponseDto?>>> GetMyPosts(Guid userId, string? cursor, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching posts of User: {UserId}", userId);
            Cursor? c = null;
            if (!string.IsNullOrEmpty(cursor) && !Cursor.TryDecode(cursor, out c))
            {
                throw new Domain.Exceptions.ValidationException("Invalid cursor");
            }

            var result = await _postRepository.GetAllUserPostByUserIdAsync(c, userId, cancellationToken);
            return Result<CursorPageResult<PostListResponseDto?>>.Success(result, "User posts fetched successfully", 200);
        }

        public async Task<Result<PostResponseDto>> GetPostByIdAsync(Guid id, Guid currUserId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching post with Id: {PostId}", id);
            var post = await _postRepository.GetPostByIdAsync(id, currUserId, cancellationToken)
                       ?? throw new NotFoundException("Post", id);

            var carId = post.CarId;

            if (carId == null)
            {
                _logger.LogWarning("Car associated with Post Id: {PostId} not found", id);
                throw new NotFoundException("Car associated with the post not found");
            }

            bool viewTimeElapsed = await _viewRepository.HasViewTimeElapsedAsync(id, currUserId);
            bool hasChanges = false;

            if (viewTimeElapsed)
            {
                var view = PostView.CreatePostView(post.Id, currUserId);
                await _viewRepository.CreatePostViewAsync(view);
                hasChanges = true;

                // Get post entity and increment view count
                var postEntity = await _postRepository.GetPostEntityByIdAsync(id, cancellationToken);
                if (postEntity != null)
                {
                    postEntity.IncrementViewCount();
                    hasChanges = true;
                    // Update cache field
                    await _cacheService.UpdateHashFieldAsync($"posts:details:{id}", "ViewCount", postEntity.ViewCount);
                }
            }

            if (hasChanges)
            {
                await _commonRepository.SaveChangesAsync();
            }
            _logger.LogInformation("Post with Id: {PostId} fetched successfully", id);
            return Result<PostResponseDto>.Success(post, "Post fetched successfully", 200);
        }

        public async Task<Result<PostResponseDto>> CreatePostAsync(CreatePostRequestDto req, Guid dealerId)
        {
            if (dealerId == Guid.Empty)
                throw new Domain.Exceptions.ValidationException("Invalid dealer Id");

            _logger.LogInformation("Creating a new post for dealer with Id: {DealerId}", dealerId);

            await _createPostValidator.EnsureValidAsync(req);

            var refrencedCar = await _carRepository.GetCarByIdAsync(req.CarId);

            if (refrencedCar == null)
            {
                throw new NotFoundException("Car", req.CarId);
            }

            if (refrencedCar.DealerId != dealerId)
            {
                throw new ForbiddenException("This car does not belong to you.");
            }

            var user = await _userRepository.GetUserByIdAsync(dealerId)
                       ?? throw new NotFoundException("Dealer", dealerId);

            var post = Post.CreatePost(req.Caption, req.Content, req.Visibility, dealerId, req.CarId);
            await _postRepository.AddPostAsync(post);
            await _commonRepository.SaveChangesAsync();
            await InvalidateFeedCachesAsync();
            _logger.LogInformation("Post created successfully with Id: {PostId}", post.Id);

            return Result<PostResponseDto>.Success(null!, "Post created successfully", 201);
        }

        public async Task<Result<CursorPageResult<UserEngagementDto>>> GetPostLikersAsync(Guid postId, string? cursorString, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting all users liked for Post with Id: {PostId}", postId);
            var postEntity = await _postRepository.GetPostEntityByIdAsync(postId, cancellationToken)
                             ?? throw new NotFoundException("Post", postId);

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    throw new Domain.Exceptions.ValidationException("Invalid cursor");
                }
            }

            var likedUsers = await _postRepository.GetPostLikersAsync(postId, cursor, cancellationToken);

            return Result<CursorPageResult<UserEngagementDto>>.Success(likedUsers);
        }

        public async Task<Result<bool>> DeletePostAsync(Guid id, Guid userId)
        {
            var postEntity = await _postRepository.GetPostEntityByIdAsync(id)
                             ?? throw new NotFoundException("Post", id);

            bool userExists = await _userRepository.UserExistAsync(userId);
            if (!userExists)
                throw new NotFoundException("User", userId);

            if (postEntity.UserId != userId)
                throw new ForbiddenException();

            postEntity.SoftDelete();
            await _commonRepository.SaveChangesAsync();
            
            await _cacheService.RemoveHashAsync($"posts:details:{id}");
            await InvalidateFeedCachesAsync();

            _logger.LogInformation("Post with Id: {PostId} deleted", id);
            return Result<bool>.Success(true, "Post deleted successfully", 200);
        }

        public async Task<Result<string>> UpdatePostAsync(Guid id, Guid currUserId, UpdatePostDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Caption) && string.IsNullOrWhiteSpace(dto.Content) &&
                dto.Visibility == PostVisibility.Default)
                throw new Domain.Exceptions.ValidationException("Atleast 1 field is required to update.");

            var postEntity = await _postRepository.GetPostEntityByIdAsync(id)
                             ?? throw new NotFoundException("Post", id);
            
            bool userExists = await _userRepository.UserExistAsync(currUserId);
            if (!userExists)
                throw new NotFoundException("User", currUserId);

            if (postEntity.UserId != currUserId)
                throw new ForbiddenException();

            postEntity.UpdateContent(dto.Caption, dto.Content, dto.Visibility);
            await _commonRepository.SaveChangesAsync();

            // Update individual fields in cache
            var cacheKey = $"posts:details:{id}";
            if (!string.IsNullOrWhiteSpace(dto.Caption))
                await _cacheService.UpdateHashFieldAsync(cacheKey, nameof(PostListResponseDto.Caption), dto.Caption);
            if (!string.IsNullOrWhiteSpace(dto.Content))
                await _cacheService.UpdateHashFieldAsync(cacheKey, nameof(PostListResponseDto.Content), dto.Content);
            if (dto.Visibility != PostVisibility.Default)
                await _cacheService.UpdateHashFieldAsync(cacheKey, nameof(PostListResponseDto.Visibility), dto.Visibility);

            _logger.LogInformation("Post with Id: {PostId} updated in DB and cache", id);
            return Result<string>.Success(null!, "Post updated successfully", 200);
        }

        private async Task<string> BuildFeedCacheKeyAsync(string scope, Guid userId, string? cursorOrFilter)
        {
            var version = await GetOrCreateCacheVersionAsync(FeedVersionScope);
            var hash = HashValue(cursorOrFilter ?? "none");
            return $"posts:{scope}:u:{userId}:v:{version}:h:{hash}";
        }

        private async Task InvalidateFeedCachesAsync()
        {
            await _cacheService.RemoveAsync(FeedVersionScope);
        }

        private async Task<string> GetOrCreateCacheVersionAsync(string scope)
        {
            var key = $"{scope}";
            var version = await _cacheService.GetAsync<string>(key);
            if (!string.IsNullOrWhiteSpace(version))
            {
                return version;
            }

            version = Guid.NewGuid().ToString("N");
            await _cacheService.SetAsync(key, version, FeedVersionTtl);
            return version;
        }

        private static string HashValue(string value)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value));
                return Convert.ToHexString(bytes).ToLowerInvariant();
            }
        }
    }
}
