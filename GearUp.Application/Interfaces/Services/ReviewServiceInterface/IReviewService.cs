using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.Review;

namespace GearUp.Application.Interfaces.Services.ReviewServiceInterface
{
    public interface IReviewService
    {
        Task<Result<ReviewResponseDto>> CreateReviewAsync(CreateReviewRequestDto dto, Guid reviewerId);
        Task<Result<ReviewResponseDto>> UpdateReviewAsync(Guid reviewId, UpdateReviewRequestDto dto, Guid reviewerId);
        Task<Result<bool>> DeleteReviewAsync(Guid reviewId, Guid reviewerId);
        Task<Result<ReviewResponseDto>> GetReviewByIdAsync(Guid reviewId);
        Task<Result<List<ReviewResponseDto>>> GetDealerReviewsAsync(Guid dealerId);
        Task<Result<List<ReviewResponseDto>>> GetMyReviewsAsync(Guid reviewerId);
        Task<Result<DealerRatingSummaryDto>> GetDealerRatingSummaryAsync(Guid dealerId);
    }
}
