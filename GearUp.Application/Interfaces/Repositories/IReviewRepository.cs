using GearUp.Application.ServiceDtos.Review;
using GearUp.Domain.Entities.Users;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface IReviewRepository
    {
        Task AddAsync(UserReview review);
        Task<UserReview?> GetByIdAsync(Guid reviewId);
        Task<UserReview?> GetByAppointmentIdAsync(Guid appointmentId);
        Task<List<ReviewResponseDto>> GetReviewsByDealerIdAsync(Guid dealerId);
        Task<List<ReviewResponseDto>> GetReviewsByReviewerIdAsync(Guid reviewerId);
        Task<DealerRatingSummaryDto?> GetDealerRatingSummaryAsync(Guid dealerId);
        Task<bool> HasReviewForAppointmentAsync(Guid appointmentId);
        void Remove(UserReview review);
    }
}
