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
        public async Task<PostResponseDto?> GetPostByIdAsync(Guid postId)
        {
            return await _db.Posts
                .Where(p => p.Id == postId)
                .Select(p => new PostResponseDto
                {
                    Id = p.Id,
                    Caption = p.Caption,
                    Content = p.Content,
                    Visibility = p.Visibility,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    IsLikedByCurrentUser = false,

                    CarDto = p.CarId == null ? null : new CreateCarResponseDto
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
                    }
                })
                .FirstOrDefaultAsync();
        }
        public async Task<PostCountsDto> GetCountsForPostById(Guid postId, Guid userId)
        {
            return await _db.Posts.Where(p => p.Id == postId).Select(p => new PostCountsDto
            {
                LikeCount = p.Likes.Count(),
                CommentCount = p.Comments.Count(),
                ViewCount = p.Views.Count(),
                IsLikedByCurrentUser = p.Likes.Any(pl => pl.LikedUserId == userId)
            }).FirstAsync();

        }
        public async Task<Dictionary<Guid, PostCountsDto>> GetCountsForPostsById(List<Guid> postIds, Guid userId)
        {
            return await _db.Posts.Where(p => postIds.Contains(p.Id)).Select(p => new PostCountsDto
            {
                PostId = p.Id,
                LikeCount = p.Likes.Count(),
                CommentCount = p.Comments.Count(),
                ViewCount = p.Views.Count(),
                IsLikedByCurrentUser = p.Likes.Any(pl => pl.LikedUserId == userId)
            }).ToDictionaryAsync(k => k.PostId);
        }
        public async Task<int> GetPostViewCountAsync(Guid postId)
        {
            return await _db.PostViews.CountAsync(pv => pv.PostId == postId);
        }
        public async Task<PageResult<Post>> GetAllPostsAsync(int pageNum)
        {
            return new PageResult<Post>
            {
                CurrentPage = pageNum,
                PageSize = 10,
                TotalCount = await _db.Posts.CountAsync(),
                TotalPages = (int)Math.Ceiling((double)await _db.Posts.CountAsync() / 10),
                Items = await _db.Posts
                .AsNoTracking()
                      .OrderByDescending(p => p.CreatedAt)
                      .Skip((pageNum - 1) * 10)
                      .Take(10)
                      .ToListAsync()
            };

        }

        public async Task<Post?> GetPostEntityByIdAsync(Guid postId)
        {
            return await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId);
        }
    }
}
