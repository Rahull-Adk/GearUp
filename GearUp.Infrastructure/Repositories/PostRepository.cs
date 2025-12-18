using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Domain.Entities.Posts;
using GearUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
            return await _db.Posts
                .AsNoTracking()
                .Where(p => p.Id == postId)
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
                    CarDto = p.CarId == null ? null : new CarResponseDto
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
                        CarImages = p.Car.Images.Select(ci => new CarImageDto
                        {
                            Id = ci.Id,
                            Url = ci.Url
                        }).ToList(),
                        EngineCapacity = p.Car.EngineCapacity,
                        FuelType = p.Car.FuelType,
                        CarCondition = p.Car.Condition,
                        TransmissionType = p.Car.Transmission,
                        VIN = p.Car.VIN,
                    },
                    LikeCount = p.Likes.Count,
                    CommentCount = p.Comments.Count,
                    ViewCount = p.Views.Count
                })
                .FirstOrDefaultAsync();
        }

        public async Task<PostCountsDto> GetCountsForPostById(Guid postId, Guid userId)
        {
            return await _db.Posts.Where(p => p.Id == postId).Select(p => new PostCountsDto
            {
                LikeCount = p.Likes.Count,
                CommentCount = p.Comments.Count,
                ViewCount = p.Views.Count,
                IsLikedByCurrentUser = p.Likes.Any(pl => pl.LikedUserId == userId)
            }).FirstAsync();

        }

        public async Task<int> GetPostViewCountAsync(Guid postId)
        {
            return await _db.PostViews.CountAsync(pv => pv.PostId == postId);
        }

        public async Task<PageResult<PostResponseDto>> GetAllPostsAsync(int pageNum, Guid currUserId)
        {
            const int pageSize = 10;

            var query = _db.Posts.AsNoTracking();

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNum - 1) * pageSize)
                .Take(pageSize)
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

                    CarDto = p.CarId == null ? null : new CarResponseDto
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
                        VIN = p.Car.VIN,
                        CarImages = p.Car.Images
                            .Select(i => new CarImageDto
                            {
                                Id = i.Id,
                                Url = i.Url
                            })
                            .ToList()
                    }
                })
                .ToListAsync();

            return new PageResult<PostResponseDto>
            {
                CurrentPage = pageNum,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Items = items
            };
        }

        public async Task<Post?> GetPostEntityByIdAsync(Guid postId)
        {
            return await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId);
        }

        public async Task<bool> PostExistAsync(Guid PostId)
        {
           return await _db.Posts.AnyAsync(p => p.Id == PostId);
        }
    }
}
