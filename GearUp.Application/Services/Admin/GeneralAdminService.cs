using AutoMapper;
using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.Interfaces;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.AdminServiceInterface;
using GearUp.Application.ServiceDtos;
using GearUp.Application.ServiceDtos.Admin;
using GearUp.Domain.Entities;
using GearUp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GearUp.Application.Services.Admin
{
    public class GeneralAdminService : IGeneralAdminService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GeneralAdminService> _logger;
        private readonly IRealTimeNotifier _realTimeNotifier;

        public GeneralAdminService(IAdminRepository adminRepository, IUserRepository userRepository, ILogger<GeneralAdminService> logger, IRealTimeNotifier realTimeNotifier)
        {
            _adminRepository = adminRepository;
            _userRepository = userRepository;
            _logger = logger;
            _realTimeNotifier = realTimeNotifier;
        }

        public async Task<Result<CursorPageResult<ToAdminKycResponseDto>>> GetAllKycs(string? cursorString)
        {
            _logger.LogInformation("Fetching all KYC submissions");

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    return Result<CursorPageResult<ToAdminKycResponseDto>>.Failure("Invalid cursor", 400);
                }
            }

            var kycs = await _adminRepository.GetAllKycSubmissionsAsync(cursor);

            _logger.LogInformation("KYC submissions retrieved successfully");
            return Result<CursorPageResult<ToAdminKycResponseDto>>.Success(kycs, "KYC submissions retrieved successfully", 200);
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

        public async Task<Result<CursorPageResult<ToAdminKycResponseDto>>> GetKycsByStatus(KycStatus status, string? cursorString)
        {
            _logger.LogInformation("Fetching KYC submissions with status: {Status}", status);

            if (status != KycStatus.Approved && status != KycStatus.Pending && status != KycStatus.Rejected)
            {
                return Result<CursorPageResult<ToAdminKycResponseDto>>.Failure("Invalid KYC status", 400);
            }

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    return Result<CursorPageResult<ToAdminKycResponseDto>>.Failure("Invalid cursor", 400);
                }
            }

            var kycs = await _adminRepository.GetKycSubmissionsByStatusAsync(status, cursor);
            _logger.LogInformation("KYC submissions retrieved successfully");
            return Result<CursorPageResult<ToAdminKycResponseDto>>.Success(kycs, "KYC submissions retrieved successfully", 200);
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

            // Send real-time notification to the dealer about their KYC status
            var statusMessage = status switch
            {
                KycStatus.Approved => "Your KYC has been approved! You are now a dealer.",
                KycStatus.Rejected => $"Your KYC has been rejected. Reason: {rejectionReason ?? "No reason provided"}",
                _ => "Your KYC status has been updated."
            };

            var notification = new NotificationDto
            {
                Id = Guid.NewGuid(),
                Title = statusMessage,
                NotificationType = NotificationEnum.KycInfo,
                ReceiverUserId = userId,
                ActorUserId = reviewerId,
                KycId = kycId,
                SentAt = DateTime.UtcNow
            };

            await _realTimeNotifier.PushNotification(userId, notification);

            _logger.LogInformation("KYC submission status updated successfully");
            return Result<string>.Success(null!, "KYC status updated successfully", 200);

        }
    }
}
