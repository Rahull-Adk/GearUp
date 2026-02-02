using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.ReviewServiceInterface;
using GearUp.Application.ServiceDtos.Review;
using GearUp.Domain.Entities.Users;
using GearUp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GearUp.Application.Services.Reviews
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICommonRepository _commonRepository;
        private readonly ILogger<ReviewService> _logger;

        public ReviewService(
            IReviewRepository reviewRepository,
            IAppointmentRepository appointmentRepository,
            IUserRepository userRepository,
            ICommonRepository commonRepository,
            ILogger<ReviewService> logger)
        {
            _reviewRepository = reviewRepository;
            _appointmentRepository = appointmentRepository;
            _userRepository = userRepository;
            _commonRepository = commonRepository;
            _logger = logger;
        }

        public async Task<Result<ReviewResponseDto>> CreateReviewAsync(CreateReviewRequestDto dto, Guid reviewerId)
        {
            _logger.LogInformation("User {ReviewerId} creating review for dealer {DealerId}", reviewerId, dto.DealerId);

            if (dto.Rating is < 1 or > 5)
            {
                return Result<ReviewResponseDto>.Failure("Rating must be between 1 and 5.", 400);
            }

            var dealer = await _userRepository.GetUserByIdAsync(dto.DealerId);
            if (dealer == null)
            {
                return Result<ReviewResponseDto>.Failure("Dealer not found.", 404);
            }

            if (dealer.Role != UserRole.Dealer)
            {
                return Result<ReviewResponseDto>.Failure("You can only review dealers.", 400);
            }

            // Check if user has at least one completed appointment with this dealer
            var hasCompletedAppointment = await _appointmentRepository.HasCompletedAppointmentWithDealerAsync(reviewerId, dto.DealerId);
            if (!hasCompletedAppointment)
            {
                return Result<ReviewResponseDto>.Failure("You can only review dealers you have completed an appointment with.", 403);
            }

            // Check if user has already reviewed this dealer
            if (await _reviewRepository.HasReviewedDealerAsync(reviewerId, dto.DealerId))
            {
                return Result<ReviewResponseDto>.Failure("You have already reviewed this dealer.", 409);
            }

            var reviewer = await _userRepository.GetUserByIdAsync(reviewerId);
            if (reviewer == null)
            {
                return Result<ReviewResponseDto>.Failure("User information not found.", 404);
            }

            var review = UserReview.Create(
                reviewerId,
                dto.DealerId,
                dto.ReviewText,
                dto.Rating
            );

            await _reviewRepository.AddAsync(review);
            await _commonRepository.SaveChangesAsync();

            var responseDto = new ReviewResponseDto
            {
                Id = review.Id,
                ReviewerId = review.ReviewerId,
                ReviewerName = reviewer.Name,
                ReviewerAvatarUrl = reviewer.AvatarUrl,
                RevieweeId = review.RevieweeId,
                RevieweeName = dealer.Name,
                ReviewText = review.ReviewText,
                Rating = review.Rating,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt
            };

            _logger.LogInformation("Review {ReviewId} created successfully for dealer {DealerId}", review.Id, dto.DealerId);
            return Result<ReviewResponseDto>.Success(responseDto, "Review created successfully.", 201);
        }

        public async Task<Result<ReviewResponseDto>> UpdateReviewAsync(Guid reviewId, UpdateReviewRequestDto dto, Guid reviewerId)
        {
            _logger.LogInformation("User {ReviewerId} updating review {ReviewId}", reviewerId, reviewId);

            if (dto.Rating is < 1 or > 5)
            {
                return Result<ReviewResponseDto>.Failure("Rating must be between 1 and 5.", 400);
            }

            var review = await _reviewRepository.GetByIdAsync(reviewId);
            if (review == null)
            {
                return Result<ReviewResponseDto>.Failure("Review not found.", 404);
            }

            if (review.ReviewerId != reviewerId)
            {
                return Result<ReviewResponseDto>.Failure("You can only update your own reviews.", 403);
            }

            review.Update(dto.ReviewText, dto.Rating);
            await _commonRepository.SaveChangesAsync();

            var reviewer = await _userRepository.GetUserByIdAsync(review.ReviewerId);
            var dealer = await _userRepository.GetUserByIdAsync(review.RevieweeId);

            var responseDto = new ReviewResponseDto
            {
                Id = review.Id,
                ReviewerId = review.ReviewerId,
                ReviewerName = reviewer?.Name ?? "Unknown",
                ReviewerAvatarUrl = reviewer?.AvatarUrl,
                RevieweeId = review.RevieweeId,
                RevieweeName = dealer?.Name ?? "Unknown",
                ReviewText = review.ReviewText,
                Rating = review.Rating,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt
            };

            _logger.LogInformation("Review {ReviewId} updated successfully", reviewId);
            return Result<ReviewResponseDto>.Success(responseDto, "Review updated successfully.");
        }

        public async Task<Result<bool>> DeleteReviewAsync(Guid reviewId, Guid reviewerId)
        {
            _logger.LogInformation("User {ReviewerId} deleting review {ReviewId}", reviewerId, reviewId);

            var review = await _reviewRepository.GetByIdAsync(reviewId);
            if (review == null)
            {
                return Result<bool>.Failure("Review not found.", 404);
            }

            if (review.ReviewerId != reviewerId)
            {
                return Result<bool>.Failure("You can only delete your own reviews.", 403);
            }

            _reviewRepository.Remove(review);
            await _commonRepository.SaveChangesAsync();

            _logger.LogInformation("Review {ReviewId} deleted successfully", reviewId);
            return Result<bool>.Success(true, "Review deleted successfully.");
        }

        public async Task<Result<ReviewResponseDto>> GetReviewByIdAsync(Guid reviewId)
        {
            var review = await _reviewRepository.GetByIdAsync(reviewId);
            if (review == null)
            {
                return Result<ReviewResponseDto>.Failure("Review not found.", 404);
            }

            var reviewer = await _userRepository.GetUserByIdAsync(review.ReviewerId);
            var dealer = await _userRepository.GetUserByIdAsync(review.RevieweeId);

            var responseDto = new ReviewResponseDto
            {
                Id = review.Id,
                ReviewerId = review.ReviewerId,
                ReviewerName = reviewer?.Name ?? "Unknown",
                ReviewerAvatarUrl = reviewer?.AvatarUrl,
                RevieweeId = review.RevieweeId,
                RevieweeName = dealer?.Name ?? "Unknown",
                ReviewText = review.ReviewText,
                Rating = review.Rating,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt
            };

            return Result<ReviewResponseDto>.Success(responseDto, "Review retrieved successfully.");
        }

        public async Task<Result<CursorPageResult<ReviewResponseDto>>> GetDealerReviewsAsync(Guid dealerId, string? cursorString)
        {
            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    return Result<CursorPageResult<ReviewResponseDto>>.Failure("Invalid cursor", 400);
                }
            }

            var reviews = await _reviewRepository.GetReviewsByDealerIdAsync(dealerId, cursor);
            return Result<CursorPageResult<ReviewResponseDto>>.Success(reviews, "Reviews retrieved successfully.");
        }

        public async Task<Result<CursorPageResult<ReviewResponseDto>>> GetMyReviewsAsync(Guid reviewerId, string? cursorString)
        {
            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    return Result<CursorPageResult<ReviewResponseDto>>.Failure("Invalid cursor", 400);
                }
            }

            var reviews = await _reviewRepository.GetReviewsByReviewerIdAsync(reviewerId, cursor);
            return Result<CursorPageResult<ReviewResponseDto>>.Success(reviews, "Reviews retrieved successfully.");
        }

        public async Task<Result<DealerRatingSummaryDto>> GetDealerRatingSummaryAsync(Guid dealerId)
        {
            var dealer = await _userRepository.GetUserByIdAsync(dealerId);
            if (dealer == null)
            {
                return Result<DealerRatingSummaryDto>.Failure("Dealer not found.", 404);
            }

            var summary = await _reviewRepository.GetDealerRatingSummaryAsync(dealerId) ?? new DealerRatingSummaryDto
            {
                DealerId = dealerId,
                DealerName = dealer.Name,
                AverageRating = 0,
                TotalReviews = 0
            };

            return Result<DealerRatingSummaryDto>.Success(summary, "Rating summary retrieved successfully.");
        }
    }
}
