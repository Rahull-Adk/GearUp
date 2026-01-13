using AutoMapper;
using FluentValidation;
using GearUp.Application.Common;
using GearUp.Application.Interfaces;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
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

        public PostService(ILogger<IPostService> logger, IValidator<CreatePostRequestDto> createPostValidator, ICommonRepository commonRepository, ICarRepository carRepository, IPostRepository postRepository,  IUserRepository userRepository, IViewRepository viewRepository)
        {
            _logger = logger;
            _createPostValidator = createPostValidator;
            _commonRepository = commonRepository;
            _carRepository = carRepository;
            _postRepository = postRepository;
            _userRepository = userRepository;
            _viewRepository = viewRepository;
        }

        public async Task<Result<PageResult<PostResponseDto>>> GetAllPostsAsync(Guid userId, int pageNum)
        {
            _logger.LogInformation("Fetching page {PageNum} of posts for user: {UserId}", pageNum, userId);
            var postsPaged = await _postRepository.GetAllPostsAsync(pageNum, userId);
            if (postsPaged.TotalCount == 0)
                return Result<PageResult<PostResponseDto>>.Success(postsPaged, "No post yet.");

            _logger.LogInformation("Posts fetched successfully from database");

            return Result<PageResult<PostResponseDto>>.Success(postsPaged, "Post fecthed successfully.");
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
            }

            await _commonRepository.SaveChangesAsync();
            post.ViewCount = await _postRepository.GetPostViewCountAsync(post.Id);
            _logger.LogInformation("Post with Id: {PostId} fetched successfully", id);
            return Result<PostResponseDto>.Success(post, "Post fetched successfully", 200);
        }

        public async Task<Result<PostResponseDto>> CreatePostAsync(CreatePostRequestDto req, Guid dealerId)
        {
            if (dealerId == Guid.Empty)
                return Result<PostResponseDto>.Failure("Invalid dealer Id", 400);

            _logger.LogInformation("Creating a new post for dealer with Id: {DealerId}", dealerId);

            var validator = _createPostValidator.Validate(req);

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

        public async Task<Result<PageResult<UserEngagementDto>>> GetPostLikersAsync(Guid postId, int pageNum)
        {
            _logger.LogInformation("Getting all users liked for Post with Id: {PostId}", postId);
            var postEntity = await _postRepository.GetPostEntityByIdAsync(postId);
            if (postEntity == null)
                return Result<PageResult<UserEngagementDto>>.Failure("Post not found", 404);

            var likedUsers = await _postRepository.GetPostLikersAsync(postId, pageNum);

            return Result<PageResult<UserEngagementDto>>.Success(likedUsers);
        }

        public async Task<Result<bool>> DeletePostAsync(Guid id)
        {
            var postEntity = await _postRepository.GetPostEntityByIdAsync(id);
            if (postEntity == null)
                return Result<bool>.Failure("Post not found", 404);

            postEntity.SoftDelete();
            await _commonRepository.SaveChangesAsync();

            _logger.LogInformation("Post with Id: {PostId} deleted", id);
            return Result<bool>.Success(true, "Post deleted successfully", 200);
        }

        public async Task<Result<string>> UpdatePostAsync(Guid id, string updatedContent)
        {
            if (string.IsNullOrWhiteSpace(updatedContent))
                return Result<string>.Failure("Updated content is invalid", 400);

            var postEntity = await _postRepository.GetPostEntityByIdAsync(id);
            if (postEntity == null)
                return Result<string>.Failure("Post not found", 404);

            postEntity.UpdateContent(postEntity.Caption, updatedContent);
            await _commonRepository.SaveChangesAsync();

            _logger.LogInformation("Post with Id: {PostId} updated", id);
            return Result<string>.Success(null!, "Post updated successfully", 200);
        }
    }
}