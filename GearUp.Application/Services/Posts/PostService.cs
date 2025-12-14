using AutoMapper;
using FluentValidation;
using GearUp.Application.Common;
using GearUp.Application.Interfaces;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.PostServiceInterface;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Domain.Entities.Posts;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace GearUp.Application.Services.Posts
{
    public class PostService : IPostService
    {
        private readonly ILogger<IPostService> _logger;
        private readonly ICacheService _cache;
        private readonly IValidator<CreatePostRequestDto> _createPostValidator;
        private readonly IUserRepository _userRepository;
        private readonly ICommonRepository _commonRepository;
        private readonly ICarRepository _carRepository;
        private readonly IPostRepository _postRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly IMapper _mapper;
        private readonly IRealTimeNotifier _realTimeNotifier;

        public PostService(ILogger<IPostService> logger, ICacheService cache, IValidator<CreatePostRequestDto> createPostValidator, ICommonRepository commonRepository, ICarRepository carRepository, IPostRepository postRepository, IMapper mapper, IUserRepository userRepository, IRealTimeNotifier realTimeNotifier, ICommentRepository commentRepository)
        {
            _logger = logger;
            _cache = cache;
            _createPostValidator = createPostValidator;
            _commonRepository = commonRepository;
            _carRepository = carRepository;
            _postRepository = postRepository;
            _mapper = mapper;
            _userRepository = userRepository;
            _realTimeNotifier = realTimeNotifier;
            _commentRepository = commentRepository;
        }
        public async Task<PageResult<PostResponseDto>> GetAllPostsAsync(Guid userId, int pageNum)
        {
            _logger.LogInformation($"Fetching page {pageNum} of posts for user: {userId}");
            var cacheKey = $"posts:all:page:{pageNum}";

            
            var cachedPosts = await _cache.GetAsync<PageResult<PostResponseDto>>(cacheKey);
            if (cachedPosts != null)
            {
                _logger.LogInformation("Posts fetched successfully from cache");
                return cachedPosts;
            }
          
            var postsPaged = await _postRepository.GetAllPostsAsync(pageNum);
            if (postsPaged.TotalCount == 0)
                return new PageResult<PostResponseDto> { Items = new List<PostResponseDto>(), CurrentPage = pageNum, PageSize = 10, TotalCount = 0, TotalPages = 0 };
            var postList = postsPaged.Items;
            var postIds = postList.Select(p => p.Id).ToList();         
            var countsDict = await _postRepository.GetCountsForPostsById(postIds, userId);         
            var allComments = await _commentRepository.GetAllCommentsByPostIdsAsync(postIds);

        
            var userIds = allComments.Select(c => c.CommentedUserId).Distinct().ToList();
            var usersDict = await _userRepository.GetAllUserWithIds(userIds);


            var commentIds = allComments.Select(c => c.Id).ToList();
            var commentLikes = await _commentRepository.GetCommentsLikeCount(commentIds);
            var currentUserLikes = await _commentRepository.GetAllCommentsLikedByUser(userId, commentIds) ?? new List<Guid>();
            var userLikedSet = currentUserLikes.ToHashSet();

    
            var commentDtos = allComments.Select(c =>
            {
                usersDict.TryGetValue(c.CommentedUserId, out var user);

                return new CommentDto
                {
                    Id = c.Id,
                    PostId = c.PostId,
                    CommentedUserId = c.CommentedUserId,
                    Content = c.Content,
                    ParentCommentId = c.ParentCommentId,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    CommentedUserName = user?.Name ?? "Unknown User",
                    CommentedUserProfilePictureUrl = user?.AvatarUrl,
                    LikeCount = commentLikes.TryGetValue(c.Id, out var likeCnt) ? likeCnt : 0,
                    IsLikedByCurrentUser = userLikedSet.Contains(c.Id),
                    Replies = new List<CommentDto>()
                };
            }).ToList();

            var commentDtosByPost = commentDtos
                .GroupBy(c => c.PostId)
                .ToDictionary(g => g.Key, g =>
                {
                    var lookup = g.ToLookup(c => c.ParentCommentId);
                    var topLevel = lookup[null].OrderByDescending(c => c.CreatedAt).ToList();

                 
                    foreach (var comment in topLevel)
                    {
                        AttachReplies(comment, lookup);
                    }

                   
                    return topLevel.OrderByDescending(c => c.CreatedAt).Take(3).ToList();
                });

            var carIds = postList
                .Where(p => p.CarId.HasValue)
                .Select(p => p.CarId!.Value)
                .Distinct()
                .ToList();

            var cars = await _carRepository.GetCarsByIdsAsync(carIds);
            var carDtos = cars.ToDictionary(
                c => c.Value.Id,
                c => new CreateCarResponseDto
                {
                    Id = c.Value.Id,
                    Make = c.Value.Make,
                    Model = c.Value.Model,
                    Year = c.Value.Year,
                    Price = c.Value.Price,
                    CarImages = c.Value.Images.Select(img => new CarImageDto
                    {
                        Id = img.Id,
                        Url = img.Url,
                        CarId = img.CarId
                    }).ToList()
                }
            );

            var postDtos = postList.Select(p =>
            {
                countsDict.TryGetValue(p.Id, out var counts);
                commentDtosByPost.TryGetValue(p.Id, out var latestComments);

                carDtos.TryGetValue(p.CarId ?? Guid.Empty, out var carDto);

                return new PostResponseDto
                {
                    Id = p.Id,
                    Caption = p.Caption,
                    Content = p.Content,
                    LikeCount = counts?.LikeCount ?? 0,
                    CommentCount = counts?.CommentCount ?? 0,
                    ViewCount = counts?.ViewCount ?? 0,
                    IsLikedByCurrentUser = counts?.IsLikedByCurrentUser ?? false,
                    LatestComments = latestComments ?? new List<CommentDto>(),
                    CarDto = carDto,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                };
            }).ToList();

            var pageResult = new PageResult<PostResponseDto>
            {
                Items = postDtos,
                CurrentPage = postsPaged.CurrentPage,
                PageSize = postsPaged.PageSize,
                TotalCount = postsPaged.TotalCount,
                TotalPages = postsPaged.TotalPages
            };

            await _cache.SetAsync(cacheKey, pageResult, TimeSpan.FromMinutes(15));

            foreach (var postDto in postDtos)
            {
                var singleKey = $"post:{postDto.Id}";
                await _cache.SetAsync(singleKey, postDto, TimeSpan.FromMinutes(10));
            }
            var pagesIndexKey = "posts:all:pages";
            var existingPages = await _cache.GetAsync<List<string>>(pagesIndexKey) ?? new List<string>();
            if (!existingPages.Contains(cacheKey))
            {
                existingPages.Add(cacheKey);
                await _cache.SetAsync(pagesIndexKey, existingPages, TimeSpan.FromHours(1));
            }
            _logger.LogInformation("Posts fetched successfully from database");

            return pageResult;
        }

        private void AttachReplies(CommentDto parent, ILookup<Guid?, CommentDto> lookup)
        {
            var replies = lookup[parent.Id].OrderBy(r => r.CreatedAt).ToList();
            parent.Replies = replies;
            foreach (var reply in replies)
            {
                AttachReplies(reply, lookup);
            }
        }


        public async Task<Result<PostResponseDto>> GetPostByIdAsync(Guid id, Guid currUserId)
        {
            _logger.LogInformation("Fetching post with Id: {PostId}", id);
            var cacheKey = $"post:{id}";

            var cachedPost = await _cache.GetAsync<PostResponseDto?>(cacheKey);
            if (cachedPost is not null)
            {
                _logger.LogInformation("Post with Id: {PostId} fetched successfully from cache", id);
                return Result<PostResponseDto>.Success(cachedPost, "Post fetched successfully", 200);
            }

            var post = await _postRepository.GetPostByIdAsync(id);
            if (post == null)
            {
                _logger.LogWarning("Post with Id: {PostId} not found", id);
                return Result<PostResponseDto>.Failure("Post not found", 404);
            }

            var counts = await _postRepository.GetCountsForPostById(id, currUserId);
            var car = post.CarDto;

            if (car == null)
            {
                _logger.LogWarning("Car associated with Post Id: {PostId} not found", id);
                return Result<PostResponseDto>.Failure("Car associated with the post not found", 404);
            }

            var carImages = await _carRepository.GetCarImagesByCarIdAsync(car.Id);

            car.CarImages = carImages;

            var comments = await _commentRepository.GetAllCommentsByPostIdAsync(post.Id);

            var commentDtos = new List<CommentDto>();

            if (comments.Count != 0)
            {
                var userIds = comments.Select(c => c.CommentedUserId).Distinct().ToList();
                var usersDict = await _userRepository.GetAllUserWithIds(userIds);
                var commentIds = comments.Select(c => c.Id).ToList();
                var commentLikes = await _commentRepository.GetCommentsLikeCount(commentIds);
                var currentUserCommentLikes = await _commentRepository.GetAllCommentsLikedByUser(currUserId, commentIds)!;

                var userLikedSet = currentUserCommentLikes.ToHashSet();

                commentDtos = comments.Select(c =>
                {
                    usersDict.TryGetValue(c.CommentedUserId, out var user);

                    return new CommentDto
                    {
                        Id = c.Id,
                        PostId = c.PostId,
                        CommentedUserId = c.CommentedUserId,
                        Content = c.Content,
                        ParentCommentId = c.ParentCommentId,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        CommentedUserName = user?.Name ?? "Unknown User",
                        CommentedUserProfilePictureUrl = user!.AvatarUrl,
                        LikeCount = commentLikes.TryGetValue(c.Id, out var likeCnt) ? likeCnt : 0,
                        IsLikedByCurrentUser = userLikedSet.Contains(c.Id),
                        Replies = new List<CommentDto>(),

                    };
                }).ToList();


                var lookup = commentDtos.ToLookup(c => c.ParentCommentId);
                foreach (var comment in commentDtos)
                {
                    comment.Replies = lookup[comment.Id].ToList();
                }

                commentDtos = lookup[null].ToList();

            }

            post.LikeCount = counts.LikeCount;
            post.CommentCount = counts.CommentCount;
            post.ViewCount = counts.ViewCount;
            post.IsLikedByCurrentUser = counts.IsLikedByCurrentUser;
            post.CarDto = car;
            post.LatestComments = commentDtos;

            await _cache.SetAsync(cacheKey, post, TimeSpan.FromMinutes(15));
            _logger.LogInformation("Post with Id: {PostId} fetched successfully from database", id);
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
                return Result<PostResponseDto>.Failure("Referenced car not found or does not belong to the dealer", 404);
            }

            var user = await _userRepository.GetUserByIdAsync(dealerId);
            if (user == null)
            {
                return Result<PostResponseDto>.Failure("Dealer not found", 404);
            }

            var post = Post.CreatePost(req.Caption, req.Content, req.Visibility, dealerId, req.CarId);
            var mappedCar = _mapper.Map<CreateCarResponseDto>(refrencedCar);
            await _postRepository.AddPostAsync(post);
            await _commonRepository.SaveChangesAsync();

            var res = new PostResponseDto
            {
                Id = post.Id,
                Caption = post.Caption,
                Content = post.Content,
                Visibility = post.Visibility,
                LikeCount = 0,
                CommentCount = 0,
                ViewCount = 0,
                IsLikedByCurrentUser = false,
                CarDto = mappedCar,
                LatestComments = new List<CommentDto>(),
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt
            };

            var cacheKey = $"post:{post.Id}";
            await _cache.SetAsync(cacheKey, res, TimeSpan.FromMinutes(15));

            // invalidate paged posts cache so new post shows up
            var pagesIndexKey = "posts:all:pages";
            var pages = await _cache.GetAsync<List<string>>(pagesIndexKey);
            if (pages != null)
            {
                foreach (var pk in pages) await _cache.RemoveAsync(pk);
                await _cache.RemoveAsync(pagesIndexKey);
            }

            _logger.LogInformation("Post created successfully with Id: {PostId}", post.Id);

            return Result<PostResponseDto>.Success(res, "Post created successfully", 201);
        }

        public async Task<Result<bool>> DeletePostAsync(Guid id)
        {
            // fetch tracked post entity
            var postEntity = await _postRepository.GetPostEntityByIdAsync(id);
            if (postEntity == null)
                return Result<bool>.Failure("Post not found", 404);

            postEntity.SoftDelete();
            await _commonRepository.SaveChangesAsync();

            await _cache.RemoveAsync($"post:{id}");

            // invalidate all paged posts caches
            var pagesIndexKey = "posts:all:pages";
            var pages = await _cache.GetAsync<List<string>>(pagesIndexKey);
            if (pages != null)
            {
                foreach (var pk in pages) await _cache.RemoveAsync(pk);
                await _cache.RemoveAsync(pagesIndexKey);
            }

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

            await _cache.RemoveAsync($"post:{id}");

            // invalidate paged posts cache
            var pagesIndexKey = "posts:all:pages";
            var pages = await _cache.GetAsync<List<string>>(pagesIndexKey);
            if (pages != null)
            {
                foreach (var pk in pages) await _cache.RemoveAsync(pk);
                await _cache.RemoveAsync(pagesIndexKey);
            }

            _logger.LogInformation("Post with Id: {PostId} updated", id);
            return Result<string>.Success(updatedContent, "Post updated successfully", 200);
        }

    }
}
