using AutoMapper;
using FluentValidation;
using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.PostServiceInterface;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Application.ServiceDtos.Socials;
using GearUp.Domain.Entities.Posts;
using Microsoft.Extensions.Logging;

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
        private readonly IMapper _mapper;

        public PostService(ILogger<IPostService> logger, IValidator<CreatePostRequestDto> createPostValidator,
            ICommonRepository commonRepository, ICarRepository carRepository, IPostRepository postRepository,
            IUserRepository userRepository, IViewRepository viewRepository)
        {
            _logger = logger;
            _createPostValidator = createPostValidator;
            _commonRepository = commonRepository;
            _carRepository = carRepository;
            _postRepository = postRepository;
            _userRepository = userRepository;
            _viewRepository = viewRepository;
        }

        public async Task<Result<CursorPageResult<PostResponseDto>>> GetLatestFeedAsync(Guid userId, string? cursor)
        {
            _logger.LogInformation("Fetching page posts for user: {UserId}", userId);
            Cursor? c = null;
            if (!string.IsNullOrEmpty(cursor) && !Cursor.TryDecode(cursor, out c))
            {
                _logger.LogInformation("Invalid cursor {Cursor}", cursor);

                return Result<CursorPageResult<PostResponseDto>>.Failure("Invalid cursor format.", 400);
            }

            var postsPaged = await _postRepository.GetLatestFeedAsync(c, userId);

            _logger.LogInformation("Posts fetched successfully from database");

            return Result<CursorPageResult<PostResponseDto>>.Success(postsPaged, "Post fecthed successfully.");
        }

        public async Task<Result<CursorPageResult<PostResponseDto?>>> GetMyPosts(Guid userId, string? cursorString)
        {
            _logger.LogInformation("Fetching posts for user: {UserId}", userId);

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    return Result<CursorPageResult<PostResponseDto?>>.Failure("Invalid cursor", 400);
                }
            }

            var postsPaged = await _postRepository.GetAllUserPostByUserIdAsync(cursor, userId);

            _logger.LogInformation("Posts fetched successfully from database");

            return Result<CursorPageResult<PostResponseDto?>>.Success(postsPaged, "Post fetched successfully.");
        }


        public async Task<Result<PostResponseDto>> GetPostByIdAsync(Guid id, Guid currUserId)
        {
            _logger.LogInformation("Fetching post with Id: {PostId}", id);
            var post = await _postRepository.GetPostByIdAsync(id, currUserId);
            if (post == null)
            {
                _logger.LogWarning("Post with Id: {PostId} not found", id);
                return Result<PostResponseDto>.Failure("Post not found", 404);
            }

            var car = post.CarDto;

            if (car == null)
            {
                _logger.LogWarning("Car associated with Post Id: {PostId} not found", id);
                return Result<PostResponseDto>.Failure("Car associated with the post not found", 404);
            }

            bool viewTimeElapsed = await _viewRepository.HasViewTimeElapsedAsync(id, currUserId);

            if (viewTimeElapsed)
            {
                var view = PostView.CreatePostView(post.Id, currUserId);
                await _viewRepository.CreatePostViewAsync(view);

                // Get post entity and increment view count
                var postEntity = await _postRepository.GetPostEntityByIdAsync(id);
                if (postEntity != null)
                {
                    postEntity.IncrementViewCount();
                }
            }

            await _commonRepository.SaveChangesAsync();
            _logger.LogInformation("Post with Id: {PostId} fetched successfully", id);
            return Result<PostResponseDto>.Success(post, "Post fetched successfully", 200);
        }

        public async Task<Result<PostResponseDto>> CreatePostAsync(CreatePostRequestDto req, Guid dealerId)
        {
            if (dealerId == Guid.Empty)
                return Result<PostResponseDto>.Failure("Invalid dealer Id", 400);

            _logger.LogInformation("Creating a new post for dealer with Id: {DealerId}", dealerId);

            var validator = await _createPostValidator.ValidateAsync(req);

            if (!validator.IsValid)
            {
                var errors = string.Join(", ", validator.Errors.Select(e => e.ErrorMessage));
                return Result<PostResponseDto>.Failure($"Post creation failed due to validation errors: {errors}", 400);
            }

            var refrencedCar = await _carRepository.GetCarByIdAsync(req.CarId);

            if (refrencedCar == null || refrencedCar.DealerId != dealerId)
            {
                return Result<PostResponseDto>.Failure("Referenced car not found or does not belong to the dealer",
                    404);
            }

            var user = await _userRepository.GetUserByIdAsync(dealerId);
            if (user == null)
            {
                return Result<PostResponseDto>.Failure("Dealer not found", 404);
            }

            var post = Post.CreatePost(req.Caption, req.Content, req.Visibility, dealerId, req.CarId);
            await _postRepository.AddPostAsync(post);
            await _commonRepository.SaveChangesAsync();
            _logger.LogInformation("Post created successfully with Id: {PostId}", post.Id);

            return Result<PostResponseDto>.Success(null!, "Post created successfully", 201);
        }

        public async Task<Result<CursorPageResult<UserEngagementDto>>> GetPostLikersAsync(Guid postId, string? cursorString)
        {
            _logger.LogInformation("Getting all users liked for Post with Id: {PostId}", postId);
            var postEntity = await _postRepository.GetPostEntityByIdAsync(postId);
            if (postEntity == null)
                return Result<CursorPageResult<UserEngagementDto>>.Failure("Post not found", 404);

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    return Result<CursorPageResult<UserEngagementDto>>.Failure("Invalid cursor", 400);
                }
            }

            var likedUsers = await _postRepository.GetPostLikersAsync(postId, cursor);

            return Result<CursorPageResult<UserEngagementDto>>.Success(likedUsers);
        }

        public async Task<Result<bool>> DeletePostAsync(Guid id, Guid userId)
        {
            var postEntity = await _postRepository.GetPostEntityByIdAsync(id);
            if (postEntity == null)
                return Result<bool>.Failure("Post not found", 404);

            bool userExists = await _userRepository.UserExistAsync(userId);
            if (!userExists)
            {
                return Result<bool>.Failure("User not found", 404);
            }

            if (postEntity.UserId != userId)
            {
                return Result<bool>.Failure("Unauthorized", 403);
            }

            postEntity.SoftDelete();
            await _commonRepository.SaveChangesAsync();

            _logger.LogInformation("Post with Id: {PostId} deleted", id);
            return Result<bool>.Success(true, "Post deleted successfully", 200);
        }

        public async Task<Result<string>> UpdatePostAsync(Guid id, Guid currUserId, UpdatePostDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Caption) && string.IsNullOrWhiteSpace(dto.Content) &&
                dto.Visibility == PostVisibility.Default)
                return Result<string>.Failure("Atleast 1 field is required to update.", 400);

            var postEntity = await _postRepository.GetPostEntityByIdAsync(id);
            if (postEntity == null)
                return Result<string>.Failure("Post not found", 404);
            bool userExists = await _userRepository.UserExistAsync(currUserId);
            if (!userExists)
            {
                return Result<string>.Failure("User not found", 404);
            }

            if (postEntity.UserId != currUserId)
            {
                return Result<string>.Failure("Unauthorized", 403);
            }

            postEntity.UpdateContent(dto.Caption, dto.Content, dto.Visibility);
            await _commonRepository.SaveChangesAsync();

            _logger.LogInformation("Post with Id: {PostId} updated", id);
            return Result<string>.Success(null!, "Post updated successfully", 200);
        }
    }
}