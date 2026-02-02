using GearUp.Application.Common.Pagination;
using GearUp.Application.ServiceDtos.Review;
using GearUp.Domain.Entities.Users;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface IReviewRepository
    {
        Task AddAsync(UserReview review);
        Task<UserReview?> GetByIdAsync(Guid reviewId);
        Task<UserReview?> GetByReviewerAndDealerIdAsync(Guid reviewerId, Guid dealerId);
        Task<CursorPageResult<ReviewResponseDto>> GetReviewsByDealerIdAsync(Guid dealerId, Cursor? cursor);
        Task<CursorPageResult<ReviewResponseDto>> GetReviewsByReviewerIdAsync(Guid reviewerId, Cursor? cursor);
        Task<DealerRatingSummaryDto?> GetDealerRatingSummaryAsync(Guid dealerId);
        Task<bool> HasReviewedDealerAsync(Guid reviewerId, Guid dealerId);
        void Remove(UserReview review);
    }
}
