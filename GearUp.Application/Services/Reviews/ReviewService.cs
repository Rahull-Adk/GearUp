using GearUp.Application.Common;
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
            _logger.LogInformation("User {ReviewerId} creating review for appointment {AppointmentId}", reviewerId, dto.AppointmentId);

            if (dto.Rating is < 1 or > 5)
            {
                return Result<ReviewResponseDto>.Failure("Rating must be between 1 and 5.", 400);
            }

            var appointment = await _appointmentRepository.GetByIdAsync(dto.AppointmentId);
            if (appointment == null)
            {
                return Result<ReviewResponseDto>.Failure("Appointment not found.", 404);
            }

            if (appointment.RequesterId != reviewerId)
            {
                return Result<ReviewResponseDto>.Failure("You can only review appointments you requested.", 403);
            }

            if (appointment.Status != AppointmentStatus.Completed)
            {
                return Result<ReviewResponseDto>.Failure("You can only review completed appointments.", 400);
            }

            if (await _reviewRepository.HasReviewForAppointmentAsync(dto.AppointmentId))
            {
                return Result<ReviewResponseDto>.Failure("You have already reviewed this appointment.", 409);
            }

            var reviewer = await _userRepository.GetUserByIdAsync(reviewerId);
            var dealer = await _userRepository.GetUserByIdAsync(appointment.AgentId);

            if (reviewer == null || dealer == null)
            {
                return Result<ReviewResponseDto>.Failure("User information not found.", 404);
            }

            var review = UserReview.Create(
                reviewerId,
                appointment.AgentId,
                dto.AppointmentId,
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
                AppointmentId = review.AppointmentId,
                ReviewText = review.ReviewText,
                Rating = review.Rating,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt
            };

            _logger.LogInformation("Review {ReviewId} created successfully for appointment {AppointmentId}", review.Id, dto.AppointmentId);
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
                AppointmentId = review.AppointmentId,
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
                AppointmentId = review.AppointmentId,
                ReviewText = review.ReviewText,
                Rating = review.Rating,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt
            };

            return Result<ReviewResponseDto>.Success(responseDto, "Review retrieved successfully.");
        }

        public async Task<Result<List<ReviewResponseDto>>> GetDealerReviewsAsync(Guid dealerId)
        {
            var reviews = await _reviewRepository.GetReviewsByDealerIdAsync(dealerId);
            return Result<List<ReviewResponseDto>>.Success(reviews, "Reviews retrieved successfully.");
        }

        public async Task<Result<List<ReviewResponseDto>>> GetMyReviewsAsync(Guid reviewerId)
        {
            var reviews = await _reviewRepository.GetReviewsByReviewerIdAsync(reviewerId);
            return Result<List<ReviewResponseDto>>.Success(reviews, "Reviews retrieved successfully.");
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
