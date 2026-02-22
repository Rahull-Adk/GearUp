using GearUp.Application.Common.Pagination;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.ServiceDtos.Review;
using GearUp.Domain.Entities.Users;
using GearUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GearUp.Infrastructure.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly GearUpDbContext _db;
        private const int PageSize = 10;

        public ReviewRepository(GearUpDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(UserReview review)
        {
            await _db.UserReviews.AddAsync(review);
        }

        public async Task<UserReview?> GetByIdAsync(Guid reviewId)
        {
            return await _db.UserReviews
                .FirstOrDefaultAsync(r => r.Id == reviewId);
        }

        public async Task<UserReview?> GetByReviewerAndDealerIdAsync(Guid reviewerId, Guid dealerId)
        {
            return await _db.UserReviews
                .FirstOrDefaultAsync(r => r.ReviewerId == reviewerId && r.RevieweeId == dealerId);
        }

        public async Task<CursorPageResult<ReviewResponseDto>> GetReviewsByDealerIdAsync(Guid dealerId, Cursor? cursor)
        {
            IQueryable<UserReview> query = _db.UserReviews
                .AsNoTracking()
                .Where(r => r.RevieweeId == dealerId)
                .OrderByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.Id);

            if (cursor is not null)
            {
                query = query.Where(r => r.CreatedAt < cursor.CreatedAt ||
                                         (r.CreatedAt == cursor.CreatedAt && r.Id.CompareTo(cursor.Id) < 0));
            }

            var reviews = await query
                .Take(PageSize + 1)
                .Select(r => new ReviewResponseDto
                {
                    Id = r.Id,
                    ReviewerId = r.ReviewerId,
                    ReviewerName = r.Reviewer != null ? r.Reviewer.Name : "Unknown",
                    ReviewerAvatarUrl = r.Reviewer != null ? r.Reviewer.AvatarUrl : null,
                    RevieweeId = r.RevieweeId,
                    RevieweeName = r.Reviewee != null ? r.Reviewee.Name : "Unknown",
                    ReviewText = r.ReviewText,
                    Rating = r.Rating,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToListAsync();

            bool hasMore = reviews.Count > PageSize;
            string? nextCursor = null;

            if (hasMore)
            {
                var lastItem = reviews[PageSize - 1];
                nextCursor = Cursor.Encode(new Cursor { CreatedAt = lastItem.CreatedAt, Id = lastItem.Id });
            }

            return new CursorPageResult<ReviewResponseDto>
            {
                Items = reviews.Take(PageSize).ToList(), NextCursor = nextCursor, HasMore = hasMore
            };
        }

        public async Task<CursorPageResult<ReviewResponseDto>> GetReviewsByReviewerIdAsync(Guid reviewerId,
            Cursor? cursor)
        {
            IQueryable<UserReview> query = _db.UserReviews
                .AsNoTracking()
                .Where(r => r.ReviewerId == reviewerId)
                .OrderByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.Id);

            if (cursor is not null)
            {
                query = query.Where(r => r.CreatedAt < cursor.CreatedAt ||
                                         (r.CreatedAt == cursor.CreatedAt && r.Id.CompareTo(cursor.Id) < 0));
            }

            var reviews = await query
                .Take(PageSize + 1)
                .Select(r => new ReviewResponseDto
                {
                    Id = r.Id,
                    ReviewerId = r.ReviewerId,
                    ReviewerName = r.Reviewer != null ? r.Reviewer.Name : "Unknown",
                    ReviewerAvatarUrl = r.Reviewer != null ? r.Reviewer.AvatarUrl : null,
                    RevieweeId = r.RevieweeId,
                    RevieweeName = r.Reviewee != null ? r.Reviewee.Name : "Unknown",
                    ReviewText = r.ReviewText,
                    Rating = r.Rating,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToListAsync();

            bool hasMore = reviews.Count > PageSize;
            string? nextCursor = null;

            if (hasMore)
            {
                var lastItem = reviews[PageSize - 1];
                nextCursor = Cursor.Encode(new Cursor { CreatedAt = lastItem.CreatedAt, Id = lastItem.Id });
            }

            return new CursorPageResult<ReviewResponseDto>
            {
                Items = reviews.Take(PageSize).ToList(), NextCursor = nextCursor, HasMore = hasMore
            };
        }

        public async Task<DealerRatingSummaryDto?> GetDealerRatingSummaryAsync(Guid dealerId)
        {
            var summary = await _db.UserReviews
                .AsNoTracking()
                .Where(r => r.RevieweeId == dealerId)
                .GroupBy(r => r.RevieweeId)
                .Select(g => new DealerRatingSummaryDto
                {
                    DealerId = dealerId,
                    DealerName = g.First().Reviewee != null ? g.First().Reviewee!.Name : "Unknown",
                    AverageRating = Math.Round(g.Average(r => r.Rating), 2),
                    TotalReviews = g.Count(),
                    FiveStarCount = g.Count(r => r.Rating >= 4.5),
                    FourStarCount = g.Count(r => r.Rating >= 3.5 && r.Rating < 4.5),
                    ThreeStarCount = g.Count(r => r.Rating >= 2.5 && r.Rating < 3.5),
                    TwoStarCount = g.Count(r => r.Rating >= 1.5 && r.Rating < 2.5),
                    OneStarCount = g.Count(r => r.Rating < 1.5)
                })
                .FirstOrDefaultAsync();

            return summary;
        }

        public async Task<bool> HasReviewedDealerAsync(Guid reviewerId, Guid dealerId)
        {
            return await _db.UserReviews
                .AsNoTracking()
                .AnyAsync(r => r.ReviewerId == reviewerId && r.RevieweeId == dealerId);
        }

        public void Remove(UserReview review)
        {
            _db.UserReviews.Remove(review);
        }
    }
}