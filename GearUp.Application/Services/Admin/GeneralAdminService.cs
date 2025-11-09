using AutoMapper;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
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
        private readonly IMapper _mapper;
        private readonly ICacheService _cache;
        private readonly ILogger<GeneralAdminService> _logger;
        public GeneralAdminService(IAdminRepository adminRepository, IMapper mapper, IUserRepository userRepository, ICacheService cache, ILogger<GeneralAdminService> logger)
        {
            _adminRepository = adminRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Result<ToAdminKycListResponseDto>> GetAllKycs()
        {
            _logger.LogInformation("Fetching all KYC submissions");

            const string cacheKey = "kyc:all";
            var cachedKycs = await _cache.GetAsync<ToAdminKycListResponseDto>(cacheKey);

            if (cachedKycs != null)
            {
                _logger.LogInformation("KYC submissions retrieved from cache");
                return Result<ToAdminKycListResponseDto>.Success(cachedKycs, "KYC submissions retrieved from cache", 200);
            }

            var kycs = await _adminRepository.GetAllKycSubmissionsAsync();

            if (kycs == null || kycs.Count == 0)
            {
                _logger.LogInformation("No KYC submissions found");
                return Result<ToAdminKycListResponseDto>.Success(null!, "No KYC submissions yet", 200);
            }

            var mappedKycs = _mapper.Map<List<ToAdminKycResponseDto>>(kycs);

            await _cache.SetAsync(cacheKey, new ToAdminKycListResponseDto(mappedKycs, mappedKycs.Count));
            _logger.LogInformation("KYC submissions retrieved successfully");
            return Result<ToAdminKycListResponseDto>.Success(new ToAdminKycListResponseDto(mappedKycs, mappedKycs.Count), "KYC submissions retrieved successfully", 200);
        }

        public async Task<Result<ToAdminKycResponseDto>> GetKycById(Guid kycId)
        {
            _logger.LogInformation("Fetching KYC submission with ID: {KycId}", kycId);
            var cacheKey = $"kyc:{kycId}";

            var cachedKyc = await _cache.GetAsync<ToAdminKycResponseDto>(cacheKey);

            if(cachedKyc != null)
            {
                return Result<ToAdminKycResponseDto>.Success(cachedKyc, "KYC submission retrieved from cache", 200);
            }

            var kyc = await _adminRepository.GetKycSubmissionByIdAsync(kycId);
            if (kyc == null)
            {
                return Result<ToAdminKycResponseDto>.Failure("KYC submission not found", 404);
            }
            var mappedKyc = _mapper.Map<ToAdminKycResponseDto>(kyc);
            await _cache.SetAsync(cacheKey, mappedKyc);
            _logger.LogInformation("KYC submission retrieved successfully");
            return Result<ToAdminKycResponseDto>.Success(mappedKyc, "KYC submission retrieved successfully", 200);
        }

        public async Task<Result<ToAdminKycListResponseDto>> GetKycsByStatus(KycStatus status)
        {
            _logger.LogInformation("Fetching KYC submissions with status: {Status}", status);
            var cacheKey = $"kyc:status:{status}";
            var cachedKycs = await _cache.GetAsync<ToAdminKycListResponseDto>(cacheKey);

            if(cachedKycs != null)
            {
                return Result<ToAdminKycListResponseDto>.Success(cachedKycs, "KYC submissions retrieved from cache", 200);
            }

            if (status != KycStatus.Approved && status != KycStatus.Pending && status != KycStatus.Rejected)
            {
                return Result<ToAdminKycListResponseDto>.Failure("Invalid KYC status", 400);
            }

            var kycs = await _adminRepository.GetKycSubmissionsByStatusAsync(status);
            if (kycs == null || kycs.Count == 0)
            {
                return Result<ToAdminKycListResponseDto>.Success(null!, "No KYC submissions found with the specified status", 200);
            }
            var mappedKycs = _mapper.Map<List<ToAdminKycResponseDto>>(kycs);
            await _cache.SetAsync(cacheKey, new ToAdminKycListResponseDto(mappedKycs, mappedKycs.Count));
            _logger.LogInformation("KYC submissions retrieved successfully");
            return Result<ToAdminKycListResponseDto>.Success(new ToAdminKycListResponseDto(mappedKycs, mappedKycs.Count), "KYC submissions retrieved successfully", 200);
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

            var kyc = await _adminRepository.GetKycSubmissionByIdAsync(kycId);
            if (kyc == null)
            {
                return Result<string>.Failure("KYC submission not found", 404);
            }
            var userId = kyc.UserId;
            var user = await _userRepository.GetUserByIdAsync(userId);

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
            var cachedKey = $"kyc:status:{status}";
            await _cache.RemoveAsync(cachedKey);
            var cacheKey2 = $"user:profile:{user.Username}";
            await _cache.RemoveAsync(cacheKey2);
            _logger.LogInformation("KYC submission status updated successfully");
            return Result<string>.Success(null!, "KYC status updated successfully", 200);

        }
    }
}
