using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.AdminServiceInterface;
using GearUp.Application.ServiceDtos.Admin;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Domain.Entities;
using GearUp.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace GearUp.Application.Services.Admin
{
    public class GeneralAdminService : IGeneralAdminService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICarRepository _carRepository;
        private readonly ILogger<GeneralAdminService> _logger;
        private readonly INotificationService _notificationService;
        private readonly ICacheService _cacheService;
        private const string KycVersionScope = "admin:kyc:version";
        private static readonly TimeSpan KycCacheTtl = TimeSpan.FromSeconds(90);
        private static readonly TimeSpan VersionTtl = TimeSpan.FromMinutes(10);

        public GeneralAdminService(
            IAdminRepository adminRepository,
            IUserRepository userRepository,
            ICarRepository carRepository,
            ILogger<GeneralAdminService> logger,
            INotificationService notificationService,
            ICacheService cacheService)
        {
            _adminRepository = adminRepository;
            _userRepository = userRepository;
            _carRepository = carRepository;
            _logger = logger;
            _notificationService = notificationService;
            _cacheService = cacheService;
        }

        public async Task<Result<CursorPageResult<ToAdminKycResponseDto>>> GetAllKycs(Guid adminUserId, string? cursorString)
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

            var cacheKey = await BuildKycCacheKeyAsync("all", adminUserId, cursorString);
            var cachedKycs = await _cacheService.GetAsync<CursorPageResult<ToAdminKycResponseDto>>(cacheKey);
            if (cachedKycs != null)
            {
                return Result<CursorPageResult<ToAdminKycResponseDto>>.Success(cachedKycs, "KYC submissions retrieved successfully", 200);
            }

            var kycs = await _adminRepository.GetAllKycSubmissionsAsync(cursor);
            await _cacheService.SetAsync(cacheKey, kycs, KycCacheTtl);

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

        public async Task<Result<CursorPageResult<ToAdminKycResponseDto>>> GetKycsByStatus(Guid adminUserId, KycStatus status, string? cursorString)
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

            var cacheKey = await BuildKycCacheKeyAsync($"status:{status}", adminUserId, cursorString);
            var cachedKycs = await _cacheService.GetAsync<CursorPageResult<ToAdminKycResponseDto>>(cacheKey);
            if (cachedKycs != null)
            {
                return Result<CursorPageResult<ToAdminKycResponseDto>>.Success(cachedKycs, "KYC submissions retrieved successfully", 200);
            }

            var kycs = await _adminRepository.GetKycSubmissionsByStatusAsync(status, cursor);
            await _cacheService.SetAsync(cacheKey, kycs, KycCacheTtl);
            _logger.LogInformation("KYC submissions retrieved successfully");
            return Result<CursorPageResult<ToAdminKycResponseDto>>.Success(kycs, "KYC submissions retrieved successfully", 200);
        }

        public async Task<Result<string>> UpdateKycStatus(Guid kycId, KycStatus status, Guid reviewerId, string? rejectionReason = null)
        {
            _logger.LogInformation("Updating KYC submission status for ID: {KycId}", kycId);
            if (reviewerId == Guid.Empty && status == KycStatus.Rejected)
            {
                _logger.LogWarning("ReviewerId is empty while updating KYC submission");
                return Result<string>.Failure("ReviewerId cannot be empty when rejecting a KYC", 400);
            }

            if (status != KycStatus.Rejected && !string.IsNullOrEmpty(rejectionReason))
            {
                return Result<string>.Failure("No rejection reasons allowed!");
            }

            if (status == KycStatus.Rejected && string.IsNullOrWhiteSpace(rejectionReason))
            {
                return Result<string>.Failure("Rejection reason is required when rejecting a KYC submission", 400);
            }

            var kyc = await _adminRepository.GetKycEntityByIdAsync(kycId);
            if (kyc == null)
            {
                return Result<string>.Failure("KYC submission not found", 404);
            }
            var userId = kyc.UserId;
            var user = await _userRepository.GetUserEntityByIdAsync(userId);

            if (user == null)
            {
                return Result<string>.Failure("User associated with KYC submission not found", 404);
            }

            if (reviewerId == Guid.Empty)
            {
                return Result<string>.Failure("ReviewerId cannot be empty", 400);
            }
            if (kyc.Status != KycStatus.Pending)
            {
                return Result<string>.Failure("KYC submission has already been reviewed", 400);
            }

            kyc.UpdateStatus(status, reviewerId, rejectionReason);
            if (status == KycStatus.Approved)
                user.SetRole(UserRole.Dealer);

            await _userRepository.SaveChangesAsync();

            // Send notification to the dealer about their KYC status
            var title = "Account Update";
            var content = status switch
            {
                KycStatus.Approved => "Your KYC verification has been approved. You are now a verified dealer on GearUp.",
                KycStatus.Rejected => $"Your KYC verification has been rejected. Reason: {rejectionReason ?? "No reason provided"}. You may resubmit with corrected documents.",
                _ => "Your KYC verification status has been updated. Please check your account for details."
            };

            await _notificationService.CreateAndPushNotificationAsync(
                title,
                content,
                NotificationEnum.KycInfo,
                reviewerId,
                userId,
                kycId: kycId
            );
            await _cacheService.RemoveAsync(KycVersionScope);

            _logger.LogInformation("KYC submission status updated successfully");
            return Result<string>.Success(null!, "KYC status updated successfully", 200);
        }

        private async Task<string> BuildKycCacheKeyAsync(string scope, Guid adminUserId, string? cursor)
        {
            var version = await GetOrCreateVersionAsync();
            var hash = HashValue(cursor ?? "none");
            return $"admin:kyc:{scope}:u:{adminUserId}:v:{version}:h:{hash}";
        }

        private async Task<string> GetOrCreateVersionAsync()
        {
            var version = await _cacheService.GetAsync<string>(KycVersionScope);
            if (!string.IsNullOrWhiteSpace(version))
            {
                return version;
            }

            version = Guid.NewGuid().ToString("N");
            await _cacheService.SetAsync(KycVersionScope, version, VersionTtl);
            return version;
        }

        private static string HashValue(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        // Car methods
        public async Task<Result<CursorPageResult<CarListDto>>> GetAllCars(string? cursorString)
        {
            _logger.LogInformation("Fetching all cars for admin review");

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    return Result<CursorPageResult<CarListDto>>.Failure("Invalid cursor", 400);
                }
            }

            var cars = await _carRepository.GetAllCarsForAdminAsync(cursor);

            _logger.LogInformation("Cars retrieved successfully");
            return Result<CursorPageResult<CarListDto>>.Success(cars, "Cars retrieved successfully", 200);
        }

        public async Task<Result<CarResponseDto>> GetCarById(Guid carId)
        {
            _logger.LogInformation("Fetching car with ID: {CarId}", carId);

            var car = await _carRepository.GetCarByIdAsync(carId);
            if (car == null)
            {
                return Result<CarResponseDto>.Failure("Car not found", 404);
            }

            _logger.LogInformation("Car retrieved successfully");
            return Result<CarResponseDto>.Success(car, "Car retrieved successfully", 200);
        }

        public async Task<Result<CursorPageResult<CarListDto>>> GetCarsByDealerId(Guid dealerId, string? cursorString)
        {
            _logger.LogInformation("Fetching cars for dealer with ID: {DealerId}", dealerId);

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    return Result<CursorPageResult<CarListDto>>.Failure("Invalid cursor", 400);
                }
            }

            var dealer = await _userRepository.GetUserEntityByIdAsync(dealerId);
            if (dealer == null)
            {
                return Result<CursorPageResult<CarListDto>>.Failure("Dealer not found", 404);
            }

            var cars = await _carRepository.GetCarsByDealerIdForAdminAsync(dealerId, cursor);

            _logger.LogInformation("Cars retrieved successfully for dealer");
            return Result<CursorPageResult<CarListDto>>.Success(cars, "Cars retrieved successfully", 200);
        }

        public async Task<Result<CursorPageResult<CarListDto>>> GetCarsByValidationStatus(CarValidationStatus status, string? cursorString)
        {
            _logger.LogInformation("Fetching cars with validation status: {Status}", status);

            if (status != CarValidationStatus.Approved && status != CarValidationStatus.Pending && status != CarValidationStatus.Rejected)
            {
                return Result<CursorPageResult<CarListDto>>.Failure("Invalid car validation status", 400);
            }

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    return Result<CursorPageResult<CarListDto>>.Failure("Invalid cursor", 400);
                }
            }

            var cars = await _carRepository.GetCarsByValidationStatusAsync(status, cursor);

            _logger.LogInformation("Cars retrieved successfully");
            return Result<CursorPageResult<CarListDto>>.Success(cars, "Cars retrieved successfully", 200);
        }

        public async Task<Result<string>> UpdateCarValidationStatus(Guid carId, CarValidationStatus status, Guid reviewerId, string? rejectionReason = null)
        {
            _logger.LogInformation("Updating car validation status for ID: {CarId}", carId);

            if (status == CarValidationStatus.Default)
            {
                return Result<string>.Failure("Invalid car validation status", 400);
            }

            if (reviewerId == Guid.Empty)
            {
                return Result<string>.Failure("ReviewerId cannot be empty", 400);
            }

            if (status == CarValidationStatus.Rejected && string.IsNullOrEmpty(rejectionReason))
            {
                return Result<string>.Failure("Rejection reason is required when rejecting a car", 400);
            }

            if (status != CarValidationStatus.Rejected && !string.IsNullOrEmpty(rejectionReason))
            {
                return Result<string>.Failure("Rejection reason is only allowed when rejecting a car", 400);
            }

            var car = await _carRepository.GetCarEntityByIdAsync(carId);
            if (car == null)
            {
                return Result<string>.Failure("Car not found", 404);
            }

            if (car.ValidationStatus != CarValidationStatus.Pending)
            {
                return Result<string>.Failure("Car has already been reviewed", 400);
            }

            car.UpdateValidationStatus(status, rejectionReason);
            await _carRepository.SaveChangesAsync();

            // Send notification to the dealer about their car validation status
            var title = "Listing Update";
            var content = status switch
            {
                CarValidationStatus.Approved => $"Your car listing '{car.Title}' has been approved and is now live on GearUp.",
                CarValidationStatus.Rejected => $"Your car listing '{car.Title}' has been rejected. Reason: {rejectionReason ?? "No reason provided"}. Please review and resubmit.",
                _ => $"Your car listing '{car.Title}' status has been updated. Check your listings for details."
            };

            await _notificationService.CreateAndPushNotificationAsync(
                title,
                content,
                NotificationEnum.CarInfo,
                reviewerId,
                car.DealerId,
                carId: carId
            );

            _logger.LogInformation("Car validation status updated successfully");
            return Result<string>.Success(null!, "Car validation status updated successfully", 200);
        }
    }
}
