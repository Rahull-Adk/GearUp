using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.Admin;
using GearUp.Domain.Entities;

namespace GearUp.Application.Interfaces.Services.AdminServiceInterface
{
    public interface IGeneralAdminService
    {
        Task<Result<ToAdminKycListResponseDto>> GetAllKycs();
        Task<Result<ToAdminKycResponseDto>> GetKycById(Guid kycId);
        Task<Result<ToAdminKycListResponseDto>> GetKycsByStatus(KycStatus status);
        Task<Result<string>> UpdateKycStatus(Guid kycId, KycStatus status, Guid reviewerId, string rejectionReason);
    }
}
