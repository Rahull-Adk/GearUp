using GearUp.Application.Common.Pagination;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Application.ServiceDtos.Socials;
using GearUp.Domain.Entities.Posts;
using GearUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace GearUp.Infrastructure.Repositories
{
    public class PostRepository : IPostRepository
    {
        private readonly GearUpDbContext _db;

        private sealed class PostProjection
        {
            public Guid Id { get; init; }
            public string Caption { get; init; } = string.Empty;
            public string Content { get; init; } = string.Empty;
            public PostVisibility Visibility { get; init; }
            public Guid UserId { get; init; }
            public Guid? CarId { get; init; }
            public DateTime CreatedAt { get; init; }
            public DateTime UpdatedAt { get; init; }
            public int LikeCount { get; init; }
            public int CommentCount { get; init; }
            public int ViewCount { get; init; }
        }

        private static PostResponseDto MapPostToDto(
            PostProjection post,
            string authorUsername,
            string? authorAvatarUrl,
            bool isLikedByCurrentUser,
            CarResponseDto? carDto)
        {
            return new PostResponseDto
            {
                Id = post.Id,
                Caption = post.Caption,
                Content = post.Content,
                Visibility = post.Visibility,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                IsLikedByCurrentUser = isLikedByCurrentUser,
                AuthorUsername = authorUsername,
                AuthorAvatarUrl = authorAvatarUrl ?? string.Empty,
                CarDto = carDto,
                LikeCount = post.LikeCount,
                CommentCount = post.CommentCount,
                ViewCount = post.ViewCount
            };
        }

        private static CarResponseDto MapCarToDto(CarResponseDto car, IReadOnlyDictionary<Guid, List<CarImageDto>> carImageLookup)
        {
            carImageLookup.TryGetValue(car.Id, out var carImages);

            return new CarResponseDto
            {
                Id = car.Id,
                Make = car.Make,
                Model = car.Model,
                Year = car.Year,
                Description = car.Description,
                Title = car.Title,
                Price = car.Price,
                Color = car.Color,
                Mileage = car.Mileage,
                SeatingCapacity = car.SeatingCapacity,
                EngineCapacity = car.EngineCapacity,
                FuelType = car.FuelType,
                CarCondition = car.Condition,
                TransmissionType = car.Transmission,
                CarStatus = car.Status,
                CarValidationStatus = car.ValidationStatus,
                VIN = car.VIN,
                LicensePlate = car.LicensePlate,
                DealerId = car.DealerId,
                CreatedAt = car.CreatedAt,
                UpdatedAt = car.UpdatedAt,
                CarImages = carImages ?? new List<CarImageDto>()
            };
        }

        private static List<Guid> GetDistinctCarIds(IEnumerable<PostProjection> posts)
        {
            return posts
                .Where(p => p.CarId is not null)
                .Select(p => p.CarId!.Value)
                .Distinct()
                .ToList();
        }

        private async Task<Dictionary<Guid, (string Username, string? AvatarUrl)>> GetUserLookupAsync(IEnumerable<Guid> userIds)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<Guid, (string Username, string? AvatarUrl)>();
            }

            return await _db.Users
                .AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, u.Username, u.AvatarUrl })
                .ToDictionaryAsync(u => u.Id, u => (u.Username, (string?)u.AvatarUrl));
        }

        private async Task<Dictionary<Guid, CarResponseDto>> GetCarLookupAsync(IReadOnlyCollection<Guid> carIds)
        {
            if (carIds.Count == 0)
            {
                return new Dictionary<Guid, CarResponseDto>();
            }

            return await _db.Cars
                .AsNoTracking()
                .Where(car => carIds.Contains(car.Id))
                .Select(car => new CarResponseDto
                {
                    Id = car.Id,
                    Make = car.Make,
                    Model = car.Model,
                    Year = car.Year,
                    Description = car.Description,
                    Title = car.Title,
                    Price = car.Price,
                    Color = car.Color,
                    Mileage = car.Mileage,
                    SeatingCapacity = car.SeatingCapacity,
                    EngineCapacity = car.EngineCapacity,
                    FuelType = car.FuelType,
                    CarCondition = car.Condition,
                    TransmissionType = car.Transmission,
                    CarStatus = car.Status,
                    CarValidationStatus = car.ValidationStatus,
                    VIN = car.VIN,
                    LicensePlate = car.LicensePlate,
                    DealerId = car.DealerId,
                    CreatedAt = car.CreatedAt,
                    UpdatedAt = car.UpdatedAt,
                    CarImages = new List<CarImageDto>()
                })
                .ToDictionaryAsync(car => car.Id);
        }

        private async Task<Dictionary<Guid, List<CarImageDto>>> GetCarImageLookupAsync(IReadOnlyCollection<Guid> carIds)
        {
            if (carIds.Count == 0)
            {
                return new Dictionary<Guid, List<CarImageDto>>();
            }

            var carImages = await _db.CarImages
                .AsNoTracking()
                .Where(ci => carIds.Contains(ci.CarId))
                .Select(ci => new CarImageDto
                {
                    Id = ci.Id,
                    CarId = ci.CarId,
                    Url = ci.Url
                })
                .ToListAsync();

            return carImages
                .GroupBy(ci => ci.CarId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        private async Task<HashSet<Guid>> GetLikedPostIdSetAsync(Guid currUserId, IReadOnlyCollection<Guid> postIds)
        {
            if (postIds.Count == 0)
            {
                return new HashSet<Guid>();
            }

            var likedPostIds = await _db.PostLikes
                .AsNoTracking()
                .Where(l => l.LikedUserId == currUserId && postIds.Contains(l.PostId))
                .Select(l => l.PostId)
                .ToListAsync();

            return likedPostIds.ToHashSet();
        }

        public PostRepository(GearUpDbContext db)
        {
            _db = db;
        }

        public async Task AddPostAsync(Post post)
        {
            await _db.Posts.AddAsync(post);
        }

        public async Task<PostResponseDto?> GetPostByIdAsync(Guid postId, Guid currUserId)
        {
            var post = await _db.Posts
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.Id == postId && (p.UserId == currUserId || p.Visibility == PostVisibility.Public))
                .Select(p => new PostProjection
                {
                    Id = p.Id,
                    Caption = p.Caption,
                    Content = p.Content,
                    Visibility = p.Visibility,
                    UserId = p.UserId,
                    CarId = p.CarId,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    LikeCount = p.LikeCount,
                    CommentCount = p.CommentCount,
                    ViewCount = p.ViewCount
                })
                .FirstOrDefaultAsync();

            if (post is null)
            {
                return null;
            }

            var user = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == post.UserId)
                .Select(u => new { u.Username, u.AvatarUrl })
                .FirstOrDefaultAsync();

            if (user is null)
            {
                return null;
            }

            var isLikedByCurrentUser = await _db.PostLikes
                .AsNoTracking()
                .AnyAsync(pl => pl.PostId == post.Id && pl.LikedUserId == currUserId);

            CarResponseDto? carDto = null;
            if (post.CarId is not null)
            {
                var carIds = new List<Guid> { post.CarId.Value };
                var cars = await GetCarLookupAsync(carIds);
                var carImageLookup = await GetCarImageLookupAsync(carIds);

                if (cars.TryGetValue(post.CarId.Value, out var car))
                {
                    carDto = MapCarToDto(car, carImageLookup);
                }
            }

            return MapPostToDto(post, user.Username, user.AvatarUrl, isLikedByCurrentUser, carDto);
        }

        public async Task<CursorPageResult<PostResponseDto?>> GetAllUserPostByUserIdAsync(Cursor? c, Guid currUserId)
        {
            const int pageSize = 10;

            IQueryable<Post> query = _db.Posts
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.UserId == currUserId)
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id);

            if (c is not null)
            {
                query = query.Where(p => p.CreatedAt < c.CreatedAt || (p.CreatedAt == c.CreatedAt && p.Id.CompareTo(c.Id) < 0));
            }

            var posts = await query
                .Take(pageSize + 1)
                .Select(p => new PostProjection
                {
                    Id = p.Id,
                    Caption = p.Caption,
                    Content = p.Content,
                    Visibility = p.Visibility,
                    UserId = p.UserId,
                    CarId = p.CarId,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    LikeCount = p.LikeCount,
                    CommentCount = p.CommentCount,
                    ViewCount = p.ViewCount
                })
                .ToListAsync();

            if (posts.Count == 0)
            {
                return new CursorPageResult<PostResponseDto?>
                {
                    HasMore = false,
                    Items = [],
                    NextCursor = null
                };
            }

            var author = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == currUserId)
                .Select(u => new { u.Username, u.AvatarUrl })
                .FirstOrDefaultAsync();

            if (author is null)
            {
                return new CursorPageResult<PostResponseDto?>
                {
                    HasMore = false,
                    Items = [],
                    NextCursor = null
                };
            }

            var postIds = posts.Select(p => p.Id).ToList();
            var likedSet = await GetLikedPostIdSetAsync(currUserId, postIds);

            var carIds = GetDistinctCarIds(posts);
            var cars = await GetCarLookupAsync(carIds);
            var carImageLookup = await GetCarImageLookupAsync(carIds);

            var rows = posts.Select(p =>
            {
                CarResponseDto? carDto = null;

                if (p.CarId is not null && cars.TryGetValue(p.CarId.Value, out var car))
                {
                    carDto = MapCarToDto(car, carImageLookup);
                }

                return MapPostToDto(p, author.Username, author.AvatarUrl, likedSet.Contains(p.Id), carDto);
            }).ToList();

            bool hasMore = posts.Count > pageSize;
            string? nextCursor = null;

            if (hasMore)
            {
                var lastItem = posts[pageSize - 1];
                nextCursor = Cursor.Encode(new Cursor
                {
                    CreatedAt = lastItem.CreatedAt,
                    Id = lastItem.Id
                });
            }

            return new CursorPageResult<PostResponseDto?>
            {
                HasMore = hasMore,
                Items = rows.Take(pageSize),
                NextCursor = nextCursor
            };
        }

        public async Task<PostCountsDto?> GetCountsForPostById(Guid postId, Guid userId)
        {
            var postCounts = await _db.Posts
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.Visibility == PostVisibility.Public && p.Id == postId)
                .Select(p => new
                {
                    p.Id,
                    p.LikeCount,
                    p.CommentCount,
                    p.ViewCount
                })
                .FirstOrDefaultAsync();

            if (postCounts is null)
            {
                return null;
            }

            var isLikedByCurrentUser = await _db.PostLikes
                .AsNoTracking()
                .AnyAsync(pl => pl.PostId == postId && pl.LikedUserId == userId);

            return new PostCountsDto
            {
                PostId = postCounts.Id,
                LikeCount = postCounts.LikeCount,
                CommentCount = postCounts.CommentCount,
                ViewCount = postCounts.ViewCount,
                IsLikedByCurrentUser = isLikedByCurrentUser
            };
        }

        public async Task<int> GetPostViewCountAsync(Guid postId)
        {
            return await _db.PostViews.CountAsync(pv => pv.PostId == postId);
        }

        public async Task<CursorPageResult<PostResponseDto>> GetLatestFeedAsync(Cursor? c, Guid currUserId)
        {
            const int pageSize = 10;

            IQueryable<Post> query = _db.Posts.AsNoTracking().Where(p => !p.IsDeleted && p.Visibility == PostVisibility.Public)
                .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id);

            if (c is not null)
            {
                query = query.Where(p => p.CreatedAt < c.CreatedAt || (p.CreatedAt == c.CreatedAt && p.Id.CompareTo(c.Id) < 0) ) ;
            }

            var posts = await query
                .Take(pageSize + 1)
                .Select(p => new PostProjection
                {
                    Id = p.Id,
                    Caption = p.Caption,
                    Content = p.Content,
                    Visibility = p.Visibility,
                    UserId = p.UserId,
                    CarId = p.CarId,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    LikeCount = p.LikeCount,
                    CommentCount = p.CommentCount,
                    ViewCount = p.ViewCount
                })
                .ToListAsync();

            if (posts.Count == 0)
            {
                return new CursorPageResult<PostResponseDto>
                {
                    HasMore = false,
                    Items = [],
                    NextCursor = null
                };
            }

            var users = await GetUserLookupAsync(posts.Select(p => p.UserId));

            var carIds = GetDistinctCarIds(posts);
            var cars = await GetCarLookupAsync(carIds);
            var carImageLookup = await GetCarImageLookupAsync(carIds);

            var postIds = posts.Select(p => p.Id).ToList();
            var likedSet = await GetLikedPostIdSetAsync(currUserId, postIds);

            var result = posts.Select(p =>
            {
                if (!users.TryGetValue(p.UserId, out var user))
                {
                    return null;
                }

                CarResponseDto? carDto = null;

                if (p.CarId != null && cars.TryGetValue(p.CarId.Value, out var car))
                {
                    carDto = MapCarToDto(car, carImageLookup);
                }

                return MapPostToDto(p, user.Username, user.AvatarUrl, likedSet.Contains(p.Id), carDto);
            })
            .Where(p => p is not null)
            .Select(p => p!)
            .ToList();

            string? nextCursor = null;
            bool hasMore = posts.Count > pageSize;
            if (hasMore)
            {
                var lastItem = posts[pageSize - 1];
                nextCursor = Cursor.Encode(new Cursor
                {
                    CreatedAt = lastItem.CreatedAt,
                    Id = lastItem.Id
                });
            }
            return new CursorPageResult<PostResponseDto>
            {
                Items = result.Take(pageSize).ToList(),
                NextCursor = nextCursor,
                HasMore = hasMore
            };
        }

        public async Task<Post?> GetPostEntityByIdAsync(Guid postId)
        {
            return await _db.Posts.FirstOrDefaultAsync(p =>  !p.IsDeleted && p.Id == postId);
        }

        public async Task<CursorPageResult<UserEngagementDto>> GetPostLikersAsync(Guid postId, Cursor? cursor)
        {
            const int pageSize = 20;

            IQueryable<PostLike> query = _db.PostLikes
                .AsNoTracking()
                .Where(pl => pl.PostId == postId)
                .OrderByDescending(pl => pl.UpdatedAt)
                .ThenByDescending(pl => pl.LikedUserId);

            if (cursor is not null)
            {
                query = query.Where(pl => pl.UpdatedAt < cursor.CreatedAt ||
                    (pl.UpdatedAt == cursor.CreatedAt && pl.LikedUserId.CompareTo(cursor.Id) < 0));
            }

            var rows = await query
                .Take(pageSize + 1)
                .Select(pl => new UserEngagementDto
                {
                    UserId = pl.LikedUserId,
                    UserName = pl.LikedUser != null ? pl.LikedUser.Username : string.Empty,
                    ProfilePictureUrl = pl.LikedUser != null ? pl.LikedUser.AvatarUrl : string.Empty,
                    UpdatedAt = pl.UpdatedAt
                })
                .ToListAsync();

            bool hasMore = rows.Count > pageSize;
            string? nextCursor = null;

            if (hasMore)
            {
                var lastItem = rows[pageSize - 1];
                nextCursor = Cursor.Encode(new Cursor
                {
                    CreatedAt = lastItem.UpdatedAt,
                    Id = lastItem.UserId
                });
            }

            return new CursorPageResult<UserEngagementDto>
            {
                Items = rows.Take(pageSize).ToList(),
                NextCursor = nextCursor,
                HasMore = hasMore
            };
        }

        public async Task<bool> PostExistAsync(Guid postId)
        {
            return await _db.Posts.AnyAsync(p =>  !p.IsDeleted && p.Id == postId);
        }
    }
}
