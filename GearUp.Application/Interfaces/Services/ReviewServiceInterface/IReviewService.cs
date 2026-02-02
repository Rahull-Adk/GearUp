using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.ServiceDtos.Review;

namespace GearUp.Application.Interfaces.Services.ReviewServiceInterface
{
    public interface IReviewService
    {
        Task<Result<ReviewResponseDto>> CreateReviewAsync(CreateReviewRequestDto dto, Guid reviewerId);
        Task<Result<ReviewResponseDto>> UpdateReviewAsync(Guid reviewId, UpdateReviewRequestDto dto, Guid reviewerId);
        Task<Result<bool>> DeleteReviewAsync(Guid reviewId, Guid reviewerId);
        Task<Result<ReviewResponseDto>> GetReviewByIdAsync(Guid reviewId);
        Task<Result<CursorPageResult<ReviewResponseDto>>> GetDealerReviewsAsync(Guid dealerId, string? cursor);
        Task<Result<CursorPageResult<ReviewResponseDto>>> GetMyReviewsAsync(Guid reviewerId, string? cursor);
        Task<Result<DealerRatingSummaryDto>> GetDealerRatingSummaryAsync(Guid dealerId);
    }
}
