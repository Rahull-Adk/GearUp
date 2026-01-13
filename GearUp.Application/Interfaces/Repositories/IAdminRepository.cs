using GearUp.Application.ServiceDtos.Admin;
using GearUp.Domain.Entities;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface IAdminRepository
    {
        Task<ToAdminKycListResponseDto> GetAllKycSubmissionsAsync();
        Task<ToAdminKycListResponseDto> GetKycSubmissionsByStatusAsync(KycStatus status);
        Task<KycSubmissions?> GetKycEntityByIdAsync(Guid kycId);
        Task<ToAdminKycResponseDto?> GetKycSubmissionByIdAsync(Guid kycId);
    }
}
