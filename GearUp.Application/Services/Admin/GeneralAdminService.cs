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

namespace GearUp.Application.Services.Admin
{
    public class GeneralAdminService : IGeneralAdminService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICarRepository _carRepository;
        private readonly ILogger<GeneralAdminService> _logger;
        private readonly INotificationService _notificationService;

        public GeneralAdminService(
            IAdminRepository adminRepository,
            IUserRepository userRepository,
            ICarRepository carRepository,
            ILogger<GeneralAdminService> logger,
            INotificationService notificationService)
        {
            _adminRepository = adminRepository;
            _userRepository = userRepository;
            _carRepository = carRepository;
            _logger = logger;
            _notificationService = notificationService;
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
            var title = "KYC Status Update";
            var content = status switch
            {
                KycStatus.Approved => "Your KYC has been approved! You are now a dealer.",
                KycStatus.Rejected => $"Your KYC has been rejected. Reason: {rejectionReason ?? "No reason provided"}",
                _ => "Your KYC status has been updated."
            };

            await _notificationService.CreateAndPushNotificationAsync(
                title,
                content,
                NotificationEnum.KycInfo,
                reviewerId,
                userId,
                kycId: kycId
            );

            _logger.LogInformation("KYC submission status updated successfully");
            return Result<string>.Success(null!, "KYC status updated successfully", 200);
        }

        // Car methods
        public async Task<Result<CursorPageResult<CarResponseDto>>> GetAllCars(string? cursorString)
        {
            _logger.LogInformation("Fetching all cars for admin review");

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    return Result<CursorPageResult<CarResponseDto>>.Failure("Invalid cursor", 400);
                }
            }

            var cars = await _carRepository.GetAllCarsForAdminAsync(cursor);

            _logger.LogInformation("Cars retrieved successfully");
            return Result<CursorPageResult<CarResponseDto>>.Success(cars, "Cars retrieved successfully", 200);
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

        public async Task<Result<CursorPageResult<CarResponseDto>>> GetCarsByDealerId(Guid dealerId, string? cursorString)
        {
            _logger.LogInformation("Fetching cars for dealer with ID: {DealerId}", dealerId);

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    return Result<CursorPageResult<CarResponseDto>>.Failure("Invalid cursor", 400);
                }
            }

            var dealer = await _userRepository.GetUserEntityByIdAsync(dealerId);
            if (dealer == null)
            {
                return Result<CursorPageResult<CarResponseDto>>.Failure("Dealer not found", 404);
            }

            var cars = await _carRepository.GetCarsByDealerIdForAdminAsync(dealerId, cursor);

            _logger.LogInformation("Cars retrieved successfully for dealer");
            return Result<CursorPageResult<CarResponseDto>>.Success(cars, "Cars retrieved successfully", 200);
        }

        public async Task<Result<CursorPageResult<CarResponseDto>>> GetCarsByValidationStatus(CarValidationStatus status, string? cursorString)
        {
            _logger.LogInformation("Fetching cars with validation status: {Status}", status);

            if (status != CarValidationStatus.Approved && status != CarValidationStatus.Pending && status != CarValidationStatus.Rejected)
            {
                return Result<CursorPageResult<CarResponseDto>>.Failure("Invalid car validation status", 400);
            }

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    return Result<CursorPageResult<CarResponseDto>>.Failure("Invalid cursor", 400);
                }
            }

            var cars = await _carRepository.GetCarsByValidationStatusAsync(status, cursor);

            _logger.LogInformation("Cars retrieved successfully");
            return Result<CursorPageResult<CarResponseDto>>.Success(cars, "Cars retrieved successfully", 200);
        }

        public async Task<Result<string>> UpdateCarValidationStatus(Guid carId, CarValidationStatus status, Guid reviewerId, string? rejectionReason = null)
        {
            _logger.LogInformation("Updating car validation status for ID: {CarId}", carId);

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
            var title = "Car Listing Update";
            var content = status switch
            {
                CarValidationStatus.Approved => $"Your car listing '{car.Title}' has been approved!",
                CarValidationStatus.Rejected => $"Your car listing '{car.Title}' has been rejected. Reason: {rejectionReason ?? "No reason provided"}",
                _ => $"Your car listing '{car.Title}' status has been updated."
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
