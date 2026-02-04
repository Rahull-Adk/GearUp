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
            return await _db.Posts.AsNoTracking()
                .Where(p => p.Id == postId && (p.UserId == currUserId || p.Visibility == PostVisibility.Public))
                .Select(p => new PostResponseDto
                {
                    Id = p.Id,
                    Caption = p.Caption,
                    Content = p.Content,
                    Visibility = p.Visibility,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    IsLikedByCurrentUser = p.Likes.Any(pl => pl.LikedUserId == currUserId),
                    AuthorUsername = p.User!.Username,
                    AuthorAvatarUrl = p.User.AvatarUrl,
                    CarDto = p.CarId == null
                        ? null
                        : new CarResponseDto
                        {
                            Id = p.Car!.Id,
                            Make = p.Car.Make,
                            Model = p.Car.Model,
                            Year = p.Car.Year,
                            Description = p.Car.Description,
                            Title = p.Car.Title,
                            Price = p.Car.Price,
                            Color = p.Car.Color,
                            Mileage = p.Car.Mileage,
                            SeatingCapacity = p.Car.SeatingCapacity,
                            CarImages =
                                p.Car.Images.Select(ci => new CarImageDto { Id = ci.Id, CarId = ci.CarId, Url = ci.Url }).ToList(),
                            EngineCapacity = p.Car.EngineCapacity,
                            FuelType = p.Car.FuelType,
                            CarCondition = p.Car.Condition,
                            TransmissionType = p.Car.Transmission,
                            CarStatus = p.Car.Status,
                            CarValidationStatus = p.Car.ValidationStatus,
                            VIN = p.Car.VIN,
                            LicensePlate = p.Car.LicensePlate,
                            DealerId = p.Car.DealerId,
                            CreatedAt = p.Car.CreatedAt,
                            UpdatedAt = p.Car.UpdatedAt
                        },
                    LikeCount = p.Likes.Count,
                    CommentCount = p.Comments.Count,
                    ViewCount = p.Views.Count
                })
                .FirstOrDefaultAsync();
        }

        public async Task<CursorPageResult<PostResponseDto?>> GetAllUserPostByUserIdAsync(Cursor? c, Guid currUserId)
        {
            const int pageSize = 10;

            IQueryable<Post> query = _db.Posts
                .AsNoTracking()
                .Where(p => p.UserId == currUserId)
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id);

            if (c is not null)
            {
                query = query.Where(p => p.CreatedAt < c.CreatedAt || (p.CreatedAt == c.CreatedAt && p.Id.CompareTo(c.Id) < 0));
            }

            var rows = await query
                .Take(pageSize + 1)
                .Select(p => new PostResponseDto
                {
                    Id = p.Id,
                    Caption = p.Caption,
                    Content = p.Content,
                    Visibility = p.Visibility,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    AuthorUsername = p.User.Username,
                    AuthorAvatarUrl = p.User.AvatarUrl,
                    IsLikedByCurrentUser =
                        p.Likes.Any(l => l.LikedUserId == currUserId),
                    LikeCount = p.Likes.Count,
                    CommentCount = p.Comments.Count,
                    ViewCount = p.Views.Count,
                    CarDto = p.CarId == null
                        ? null
                        : new CarResponseDto
                        {
                            Id = p.Car!.Id,
                            Make = p.Car.Make,
                            Model = p.Car.Model,
                            Year = p.Car.Year,
                            Description = p.Car.Description,
                            Title = p.Car.Title,
                            Price = p.Car.Price,
                            Color = p.Car.Color,
                            Mileage = p.Car.Mileage,
                            SeatingCapacity = p.Car.SeatingCapacity,
                            EngineCapacity = p.Car.EngineCapacity,
                            FuelType = p.Car.FuelType,
                            CarCondition = p.Car.Condition,
                            TransmissionType = p.Car.Transmission,
                            CarStatus = p.Car.Status,
                            CarValidationStatus = p.Car.ValidationStatus,
                            VIN = p.Car.VIN,
                            LicensePlate = p.Car.LicensePlate,
                            DealerId = p.Car.DealerId,
                            CreatedAt = p.Car.CreatedAt,
                            UpdatedAt = p.Car.UpdatedAt,
                            CarImages = p.Car.Images
                                .Select(i => new CarImageDto { Id = i.Id, CarId = i.CarId, Url = i.Url })
                                .ToList()
                        }
                })
                .ToListAsync();
            bool hasMore = rows.Count > pageSize;
            string? nextCursor = null;

            if (hasMore)
            {
                var lastItem = rows[pageSize - 1];
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

        public async Task<PostCountsDto> GetCountsForPostById(Guid postId, Guid userId)
        {
            var data =  await _db.Posts.Where(p => p.Id == postId && p.Visibility == PostVisibility.Public).Select(p => new PostCountsDto
            {
                LikeCount = p.Likes.Count,
                CommentCount = p.Comments.Count,
                ViewCount = p.Views.Count,
                IsLikedByCurrentUser = p.Likes.Any(pl => pl.LikedUserId == userId)
            }).FirstOrDefaultAsync();
            return data;
        }

        public async Task<int> GetPostViewCountAsync(Guid postId)
        {
            return await _db.PostViews.CountAsync(pv => pv.PostId == postId);
        }

        public async Task<CursorPageResult<PostResponseDto>> GetLatestFeedAsync(Cursor? c, Guid currUserId)
        {
            const int pageSize = 10;

            IQueryable<Post> query = _db.Posts.AsNoTracking().Where(p => p.Visibility == PostVisibility.Public)
                .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id).AsNoTracking();

            if (c is not null)
            {
                query = query.Where(p => p.CreatedAt < c.CreatedAt || (p.CreatedAt == c.CreatedAt && p.Id.CompareTo(c.Id) < 0) ) ;
            }


            var rows = await query
                .Take(pageSize + 1)
                .Select(p => new PostResponseDto
                {
                    Id = p.Id,
                    Caption = p.Caption,
                    Content = p.Content,
                    Visibility = p.Visibility,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    AuthorUsername = p.User!.Username,
                    AuthorAvatarUrl = p.User.AvatarUrl,
                    IsLikedByCurrentUser =
                        p.Likes.Any(l => l.LikedUserId == currUserId),
                    LikeCount = p.LikeCount,
                    CommentCount = p.Comments.Count,
                    ViewCount = p.Views.Count,
                    CarDto = p.CarId == null
                        ? null
                        : new CarResponseDto
                        {
                            Id = p.Car!.Id,
                            Make = p.Car.Make,
                            Model = p.Car.Model,
                            Year = p.Car.Year,
                            Description = p.Car.Description,
                            Title = p.Car.Title,
                            Price = p.Car.Price,
                            Color = p.Car.Color,
                            Mileage = p.Car.Mileage,
                            SeatingCapacity = p.Car.SeatingCapacity,
                            EngineCapacity = p.Car.EngineCapacity,
                            FuelType = p.Car.FuelType,
                            CarCondition = p.Car.Condition,
                            TransmissionType = p.Car.Transmission,
                            CarStatus = p.Car.Status,
                            CarValidationStatus = p.Car.ValidationStatus,
                            VIN = p.Car.VIN,
                            LicensePlate = p.Car.LicensePlate,
                            DealerId = p.Car.DealerId,
                            CreatedAt = p.Car.CreatedAt,
                            UpdatedAt = p.Car.UpdatedAt,
                            CarImages = p.Car.Images
                                .Select(i => new CarImageDto { Id = i.Id, CarId = i.CarId, Url = i.Url })
                                .ToList()
                        }
                })
                .ToListAsync();
            string? nextCursor = null;
            bool hasMore = rows.Count > pageSize;
            if (hasMore)
            {
                var lastItem = rows[pageSize - 1];
                nextCursor = Cursor.Encode(new Cursor
                {
                    CreatedAt = lastItem.CreatedAt,
                    Id = lastItem.Id
                });
            }
            return new CursorPageResult<PostResponseDto>
            {
                Items = rows.Take(pageSize).ToList(),
                NextCursor = nextCursor,
                HasMore = hasMore
            };
        }

        public async Task<Post?> GetPostEntityByIdAsync(Guid postId)
        {
            return await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId);
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
                    UserName = pl.LikedUser.Username,
                    ProfilePictureUrl = pl.LikedUser.AvatarUrl,
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
            return await _db.Posts.AnyAsync(p => p.Id == postId);
        }
    }
}
