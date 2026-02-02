using GearUp.Application.Common.Pagination;
using GearUp.Application.ServiceDtos.Admin;
using GearUp.Domain.Entities;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface IAdminRepository
    {
        Task<CursorPageResult<ToAdminKycResponseDto>> GetAllKycSubmissionsAsync(Cursor? cursor);
        Task<CursorPageResult<ToAdminKycResponseDto>> GetKycSubmissionsByStatusAsync(KycStatus status, Cursor? cursor);
        Task<KycSubmissions?> GetKycEntityByIdAsync(Guid kycId);
        Task<ToAdminKycResponseDto?> GetKycSubmissionByIdAsync(Guid kycId);
    }
}
