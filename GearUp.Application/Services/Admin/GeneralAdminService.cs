using AutoMapper;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.AdminServiceInterface;
using GearUp.Application.ServiceDtos.Admin;
using GearUp.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GearUp.Application.Services.Admin
{
    public class GeneralAdminService : IGeneralAdminService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GeneralAdminService> _logger;
        public GeneralAdminService(IAdminRepository adminRepository, IUserRepository userRepository, ILogger<GeneralAdminService> logger)
        {
            _adminRepository = adminRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Result<ToAdminKycListResponseDto>> GetAllKycs()
        {
            _logger.LogInformation("Fetching all KYC submissions");

            var kycs = await _adminRepository.GetAllKycSubmissionsAsync();

            if (kycs == null || kycs.TotalCount == 0)
            {
                _logger.LogInformation("No KYC submissions found");
                return Result<ToAdminKycListResponseDto>.Success(null!, "No KYC submissions yet", 200);
            }

            _logger.LogInformation("KYC submissions retrieved successfully");
            return Result<ToAdminKycListResponseDto>.Success(kycs, "KYC submissions retrieved successfully", 200);
        }

        public async Task<Result<ToAdminKycResponseDto>> GetKycById(Guid kycId)
        {
            _logger.LogInformation("Fetching KYC submission with ID: {KycId}", kycId);

            var kyc = await _adminRepository.GetKycSubmissionByIdAsync(kycId);
            if (kyc == null)
            {
                return Result<ToAdminKycResponseDto>.Failure("KYC submission not found", 404);
            }
            _logger.LogInformation("KYC submission retrieved successfully");
            return Result<ToAdminKycResponseDto>.Success(kyc, "KYC submission retrieved successfully", 200);
        }

        public async Task<Result<ToAdminKycListResponseDto>> GetKycsByStatus(KycStatus status)
        {
            _logger.LogInformation("Fetching KYC submissions with status: {Status}", status);

            if (status != KycStatus.Approved && status != KycStatus.Pending && status != KycStatus.Rejected)
            {
                return Result<ToAdminKycListResponseDto>.Failure("Invalid KYC status", 400);
            }

            var kycs = await _adminRepository.GetKycSubmissionsByStatusAsync(status);
            if (kycs == null || kycs.TotalCount == 0)
            {
                return Result<ToAdminKycListResponseDto>.Success(null!, "No KYC submissions found with the specified status", 200);
            }
            _logger.LogInformation("KYC submissions retrieved successfully");
            return Result<ToAdminKycListResponseDto>.Success(kycs, "KYC submissions retrieved successfully", 200);
        }

        public async Task<Result<string>> UpdateKycStatus(Guid kycId, KycStatus status, Guid reviewerId, string?rejectionReason = null)
        {

            _logger.LogInformation("Updating KYC submission status for ID: {KycId}", kycId);
            if (reviewerId == Guid.Empty && status == KycStatus.Rejected)
            {
                _logger.LogWarning("ReviewerId is empty while updating KYC submission");
                return Result<string>.Failure("ReviewerId cannot be empty when rejecting a KYC", 400);
            }

            if (status != KycStatus.Rejected && !string.IsNullOrEmpty(rejectionReason))
            {
                return Result<string>.Failure("No rejection reasons allowed!" +
                    "");
            }

            var kyc = await _adminRepository.GetKycEntityByIdAsync(kycId);
            if (kyc == null)
            {
                return Result<string>.Failure("KYC submission not found", 404);
            }
            var userId = kyc.UserId;
            var user = await _userRepository.GetUserEntityByIdAsync(userId);

            if (user == null) {
                return Result<string>.Failure("User associated with KYC submission not found", 404);
            }

            if (reviewerId == Guid.Empty)
            {
                return Result<string>.Failure("ReviewerId cannot be empty", 400);
            }
            if(kyc.Status != KycStatus.Pending)
            {
                return Result<string>.Failure("KYC submission has already been reviewed", 400);
            }

            kyc.UpdateStatus(status, reviewerId, rejectionReason);
            if(status == KycStatus.Approved)
                user.SetRole(Domain.Enums.UserRole.Dealer);

            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("KYC submission status updated successfully");
            return Result<string>.Success(null!, "KYC status updated successfully", 200);

        }
    }
}
