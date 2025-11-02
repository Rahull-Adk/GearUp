using GearUp.Domain.Entities;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface IAdminRepository
    {
        Task<ICollection<KycSubmissions>> GetAllKycSubmissionsAsync();
        Task<ICollection<KycSubmissions>> GetKycSubmissionsByStatusAsync(KycStatus status);
        Task<KycSubmissions?> GetKycSubmissionByIdAsync(Guid kycId);
    }
}
