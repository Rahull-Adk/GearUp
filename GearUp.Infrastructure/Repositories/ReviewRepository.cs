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

        public async Task<UserReview?> GetByAppointmentIdAsync(Guid appointmentId)
        {
            return await _db.UserReviews
                .FirstOrDefaultAsync(r => r.AppointmentId == appointmentId);
        }

        public async Task<List<ReviewResponseDto>> GetReviewsByDealerIdAsync(Guid dealerId)
        {
            return await _db.UserReviews
                .AsNoTracking()
                .Where(r => r.RevieweeId == dealerId)
                .Include(r => r.Reviewer)
                .Include(r => r.Reviewee)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewResponseDto
                {
                    Id = r.Id,
                    ReviewerId = r.ReviewerId,
                    ReviewerName = r.Reviewer != null ? r.Reviewer.Name : "Unknown",
                    ReviewerAvatarUrl = r.Reviewer != null ? r.Reviewer.AvatarUrl : null,
                    RevieweeId = r.RevieweeId,
                    RevieweeName = r.Reviewee != null ? r.Reviewee.Name : "Unknown",
                    AppointmentId = r.AppointmentId,
                    ReviewText = r.ReviewText,
                    Rating = r.Rating,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<List<ReviewResponseDto>> GetReviewsByReviewerIdAsync(Guid reviewerId)
        {
            return await _db.UserReviews
                .AsNoTracking()
                .Where(r => r.ReviewerId == reviewerId)
                .Include(r => r.Reviewer)
                .Include(r => r.Reviewee)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewResponseDto
                {
                    Id = r.Id,
                    ReviewerId = r.ReviewerId,
                    ReviewerName = r.Reviewer != null ? r.Reviewer.Name : "Unknown",
                    ReviewerAvatarUrl = r.Reviewer != null ? r.Reviewer.AvatarUrl : null,
                    RevieweeId = r.RevieweeId,
                    RevieweeName = r.Reviewee != null ? r.Reviewee.Name : "Unknown",
                    AppointmentId = r.AppointmentId,
                    ReviewText = r.ReviewText,
                    Rating = r.Rating,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<DealerRatingSummaryDto?> GetDealerRatingSummaryAsync(Guid dealerId)
        {
            var reviews = await _db.UserReviews
                .AsNoTracking()
                .Where(r => r.RevieweeId == dealerId)
                .Include(r => r.Reviewee)
                .ToListAsync();

            if (!reviews.Any())
            {
                return null;
            }

            var dealer = reviews.First().Reviewee;

            return new DealerRatingSummaryDto
            {
                DealerId = dealerId,
                DealerName = dealer?.Name ?? "Unknown",
                AverageRating = Math.Round(reviews.Average(r => r.Rating), 2),
                TotalReviews = reviews.Count,
                FiveStarCount = reviews.Count(r => r.Rating == 5),
                FourStarCount = reviews.Count(r => r.Rating == 4),
                ThreeStarCount = reviews.Count(r => r.Rating == 3),
                TwoStarCount = reviews.Count(r => r.Rating == 2),
                OneStarCount = reviews.Count(r => r.Rating == 1)
            };
        }

        public async Task<bool> HasReviewForAppointmentAsync(Guid appointmentId)
        {
            return await _db.UserReviews
                .AsNoTracking()
                .AnyAsync(r => r.AppointmentId == appointmentId);
        }

        public void Remove(UserReview review)
        {
            _db.UserReviews.Remove(review);
        }
    }
}
