using FluentValidation;
using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.CarServiceInterface;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Domain.Entities.Cars;
using GearUp.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GearUp.Application.Services.Cars
{
    public class CarService : ICarService
    {
        private readonly IValidator<CreateCarRequestDto> _createCarValidator;
        private readonly IValidator<UpdateCarDto> _updateCarValidator;
        private readonly ILogger<CarService> _logger;
        private readonly ICarRepository _carRepository;
        private readonly ICommonRepository _commonRepository;
        private readonly ICarImageService _carImageService;
        private readonly IUserRepository _userRepository;
        private readonly ICacheService _cacheService;

        private const string CarListVersionKey = "cars:list:version";
        private static readonly TimeSpan CarListCacheTtl = TimeSpan.FromSeconds(90);
        private static readonly TimeSpan CarCountCacheTtl = TimeSpan.FromMinutes(10);

        public CarService(
            IValidator<CreateCarRequestDto> createCarValidator,
            ILogger<CarService> logger,
            ICarRepository carRepository,
            ICommonRepository commonRepository,
            ICarImageService carImageService,
            IValidator<UpdateCarDto> updateCarDtoValiator,
            IUserRepository userRepository,
            ICacheService cacheService)
        {
            _createCarValidator = createCarValidator;
            _logger = logger;
            _carRepository = carRepository;
            _commonRepository = commonRepository;
            _carImageService = carImageService;
            _updateCarValidator = updateCarDtoValiator;
            _userRepository = userRepository;
            _cacheService = cacheService;
        }

        public async Task<Result<CarResponseDto>> CreateCarAsync(CreateCarRequestDto request, Guid dealerId)
        {
            _logger.LogInformation("Car creation initiated for dealer ID: {DealerId}", dealerId);

            if (dealerId == Guid.Empty)
                return Result<CarResponseDto>.Failure("Invalid dealer ID.", 400);

            var validationResult = _createCarValidator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Car creation failed validation for dealer ID: {DealerId}. Errors: {Errors}", dealerId, errors);
                return Result<CarResponseDto>.Failure(errors, 422);
            }

            var dealerExist = await _userRepository.UserExistAsync(dealerId);
            if (!dealerExist)
            {
                _logger.LogWarning("Car creation failed: Dealer not found for ID: {DealerId}", dealerId);
                return Result<CarResponseDto>.Failure("Dealer not found.", 404);
            }

            if (request.CarImages == null || request.CarImages.Count == 0)
            {
                _logger.LogWarning("Car creation failed: No images provided for dealer ID: {DealerId}", dealerId);
                return Result<CarResponseDto>.Failure("At least one car image is required.", 422);
            }

            if (await _carRepository.IsUniqueVin(request.VIN))
            {
                _logger.LogWarning("Car creation failed: VIN {VIN} already exists for dealer ID: {DealerId}", request.VIN, dealerId);
                return Result<CarResponseDto>.Failure("A car with the provided VIN already exists.", 409);
            }
            Guid carId = Guid.NewGuid();
            var imagesResult = await _carImageService.ProcessForCreateAsync(request.CarImages, dealerId, carId);
            if (!imagesResult.IsSuccess)
            {
                return Result<CarResponseDto>.Failure(imagesResult.ErrorMessage, imagesResult.Status);
            }

            var newCar = Car.CreateForSale(
                carId,
                request.Title,
                request.Description,
                request.Model,
                request.Make,
                request.Year,
                request.Price,
                request.Color,
                request.Mileage,
                request.SeatingCapacity,
                request.EngineCapacity,
                imagesResult.Data,
                request.FuelType,
                request.CarCondition,
                request.TransmissionType,
                dealerId,
                request.VIN,
                request.LicensePlate
            );

            await _carRepository.AddCarAsync(newCar);
            await _commonRepository.SaveChangesAsync();
            await InvalidateCarListCacheAsync();
            _logger.LogInformation("Car created successfully for dealer ID: {DealerId}", dealerId);

            return Result<CarResponseDto>.Success(null!, "Car added successfully.", 201);
        }

        public async Task<Result<CursorPageResult<CarResponseDto>>> GetAllCarsAsync(string? cursorString, CancellationToken cancellationToken = default)
        {
            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    return Result<CursorPageResult<CarResponseDto>>.Failure("Invalid cursor", 400);
                }
            }

            var cacheKey = await BuildCarCacheKeyAsync("all", Guid.Empty, cursorString);
            var cachedCars = await _cacheService.GetAsync<CursorPageResult<CarResponseDto>>(cacheKey);
            if (cachedCars != null)
            {
                return Result<CursorPageResult<CarResponseDto>>.Success(cachedCars, "Cars fetched successfully", 200);
            }

            var cars = await _carRepository.GetAllCarsAsync(cursor, cancellationToken);
            await _cacheService.SetAsync(cacheKey, cars, CarListCacheTtl);

            return Result<CursorPageResult<CarResponseDto>>.Success(cars, "Cars fetched successfully", 200);
        }

        public async Task<Result<CarResponseDto>> GetCarByIdAsync(Guid carId, CancellationToken cancellationToken = default)
        {
            var car = await _carRepository.GetCarByIdAsync(carId, cancellationToken);
            if (car == null)
            {
                _logger.LogWarning("Car ID: {CarId} not found.", carId);
                return Result<CarResponseDto>.Failure("Car not found", 404);
            }
            return Result<CarResponseDto>.Success(car, "Car fetched successfully", 200);
        }

        public async Task<Result<CarResponseDto>> UpdateCarAsync(Guid carId, UpdateCarDto request, Guid dealerId)
        {
            _logger.LogInformation("Car Update initiated for car ID: {CarId} by dealer ID: {DealerId}", carId, dealerId);

            var existingCar = await _carRepository.GetCarEntityByIdAsync(carId);
            if (existingCar is null)
            {
                _logger.LogWarning("No existing car found for car ID: {CarId}", carId);
                return Result<CarResponseDto>.Failure("Car not found", 404);
            }

            if (existingCar.DealerId != dealerId)
            {
                _logger.LogWarning("Unauthorized deletion attempt for car ID: {CarId} by dealer ID: {DealerId}", carId, dealerId);
                return Result<CarResponseDto>.Failure("Unauthorized to delete this car", 403);
            }

            var validation = ValidateCarUpdate(existingCar, request, dealerId);
            if (!validation.IsSuccess)
                return validation;

            var imagesResult = await _carImageService.ProcessForUpdateAsync(existingCar!, request.CarImages, dealerId);
            if (!imagesResult.IsSuccess)
                return Result<CarResponseDto>.Failure(imagesResult.ErrorMessage, imagesResult.Status);

            existingCar!.UpdateDetails(
                request.Title,
                request.Description,
                request.Model,
                request.Make,
                request.Year,
                request.Price,
                request.Color,
                request.Mileage,
                request.SeatingCapacity,
                request.EngineCapacity,
                imagesResult.Data.Count != 0 ? imagesResult.Data : null,
                request.FuelType,
                request.CarCondition,
                request.TransmissionType
            );

            await _commonRepository.SaveChangesAsync();
            await InvalidateCarListCacheAsync();
            _logger.LogInformation("Car updated successfully for car ID: {CarId}", carId);

            return Result<CarResponseDto>.Success(null!, "Car updated successfully", 200);
        }

        public async Task<Result<string>> DeleteCarByIdAsync(Guid carId, Guid dealerId)
        {
            _logger.LogInformation("Car Deletion initiated for car ID: {CarId} by dealer ID: {DealerId}", carId, dealerId);

            var existingCar = await _carRepository.GetCarEntityByIdAsync(carId);
            if (existingCar is null)
            {
                _logger.LogWarning("No existing car found for car ID: {CarId}", carId);
                return Result<string>.Failure("Car not found", 404);
            }

            var user = await _userRepository.GetUserByIdAsync(dealerId);
            if (user == null) {
                _logger.LogWarning("Car deletion failed: Dealer not found for ID: {DealerId}", dealerId);
                return Result<string>.Failure("Dealer not found.", 404);
            }

            if (existingCar.DealerId != dealerId)
            {
                _logger.LogWarning("Unauthorized deletion attempt for car ID: {CarId} by dealer ID: {DealerId}", carId, dealerId);
                return Result<string>.Failure("Unauthorized to delete this car", 403);
            }

            existingCar.DeleteCar();
            await _commonRepository.SaveChangesAsync();
            await InvalidateCarListCacheAsync();

            _logger.LogInformation("Car deleted successfully for car ID: {CarId}", carId);
            return Result<string>.Success(default!, "Car deleted successfully", 200);

        }

        public async Task<Result<CursorPageResult<CarResponseDto>>> GetDealerCarsAsync(Guid dealerId, string? cursorString, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting cars of {DealerId}", dealerId);

            var dealer = await _userRepository.GetUserByIdAsync(dealerId);
            if (dealer == null)
            {
                _logger.LogInformation("Dealer with id {DealerId} does not exist", dealerId);
                return Result<CursorPageResult<CarResponseDto>>.Failure("Dealer not found", 404);
            }

            if (dealer.Role != UserRole.Dealer)
            {
                return Result<CursorPageResult<CarResponseDto>>.Failure(
                    "Cars are only available for dealer accounts.",
                    403
                );
            }

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    return Result<CursorPageResult<CarResponseDto>>.Failure("Invalid cursor", 400);
                }
            }

            var cars = await _carRepository.GetDealerCarsAsync(dealerId, cursor, cancellationToken);

            return Result<CursorPageResult<CarResponseDto>>.Success(cars, $"Cars fetched successfully");
        }

        private Result<CarResponseDto> ValidateCarUpdate(Car? existingCar, UpdateCarDto request, Guid dealerId)
        {
            if (existingCar == null)
                return Result<CarResponseDto>.Failure("Car not found", 404);

            if (existingCar.DealerId != dealerId)
                return Result<CarResponseDto>.Failure("Unauthorized to update this car", 403);

            var validationResult = _updateCarValidator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<CarResponseDto>.Failure(errors, 422);
            }

            return Result<CarResponseDto>.Success(null!, "Validation passed", 200);
        }

        public async Task<Result<CursorPageResult<CarResponseDto>>> SearchCarsAsync(CarSearchDto? searchDto, string? cursorString, CancellationToken cancellationToken = default)
        {
            if (searchDto == null)
            {
                return Result<CursorPageResult<CarResponseDto>>.Failure("Search criteria cannot be null", 400);
            }

            if(searchDto.Query == null && searchDto.Color == null && searchDto.MinPrice == null && searchDto.MaxPrice == null)
            {
                return Result<CursorPageResult<CarResponseDto>>.Failure("At least one search criteria must be provided", 400);
            }

            if(searchDto.MinPrice != null && searchDto.MaxPrice != null && searchDto.MinPrice > searchDto.MaxPrice)
            {
                return Result<CursorPageResult<CarResponseDto>>.Failure("MinPrice cannot be greater than MaxPrice", 400);
            }

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    return Result<CursorPageResult<CarResponseDto>>.Failure("Invalid cursor", 400);
                }
            }

            var cacheKey = await BuildCarCacheKeyAsync("search", Guid.Empty, cursorString, searchDto);
            var cachedCars = await _cacheService.GetAsync<CursorPageResult<CarResponseDto>>(cacheKey);
            if (cachedCars != null)
            {
                return Result<CursorPageResult<CarResponseDto>>.Success(cachedCars, "Cars fetched successfully", 200);
            }

            var cars = await _carRepository.SearchCarsAsync(searchDto, cursor, cancellationToken);
            await _cacheService.SetAsync(cacheKey, cars, CarListCacheTtl);
            return Result<CursorPageResult<CarResponseDto>>.Success(cars, "Cars fetched successfully", 200);
        }

        public async Task<Result<CursorPageResult<CarResponseDto>>> GetMyCarsAsync(Guid dealerId, CarValidationStatus status, string? cursorString, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting cars of {DealerId}", dealerId);

            var dealerExists = await _userRepository.GetUserByIdAsync(dealerId);
            if (dealerExists == null)
            {
                _logger.LogInformation("Dealer with id {DealerId} does not exist", dealerId);
                return Result<CursorPageResult<CarResponseDto>>.Failure("Dealer not found", 404);
            }

            if (status != CarValidationStatus.Approved && status != CarValidationStatus.Pending &&
                status != CarValidationStatus.Rejected)
            {
                _logger.LogInformation("Invalid car status");
                return Result<CursorPageResult<CarResponseDto>>.Failure("Invalid car status", 404);
            }

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    return Result<CursorPageResult<CarResponseDto>>.Failure("Invalid cursor", 400);
                }
            }

            var cacheKey = await BuildCarCacheKeyAsync("my", dealerId, cursorString, status.ToString());
            var cachedCars = await _cacheService.GetAsync<CursorPageResult<CarResponseDto>>(cacheKey);
            if (cachedCars != null)
            {
                return Result<CursorPageResult<CarResponseDto>>.Success(cachedCars, $"{status} cars fetched successfully");
            }

            var cars = await _carRepository.GetMyCarsAsync(dealerId, status, cursor, cancellationToken);
            await _cacheService.SetAsync(cacheKey, cars, CarListCacheTtl);

            return Result<CursorPageResult<CarResponseDto>>.Success(cars, $"{status} cars fetched successfully");

        }

        private async Task InvalidateCarListCacheAsync()
        {
            await _cacheService.RemoveAsync(CarListVersionKey);
        }

        private async Task<string> BuildCarCacheKeyAsync(string scope, Guid userId, string? cursorOrFilter, object? filter = null)
        {
            var version = await GetOrCreateVersionAsync(CarListVersionKey);
            var serializedFilter = filter == null ? cursorOrFilter ?? "none" : JsonSerializer.Serialize(filter);
            var hash = HashValue($"{cursorOrFilter}|{serializedFilter}");
            return $"cars:{scope}:u:{userId}:v:{version}:h:{hash}";
        }

        private async Task<string> GetOrCreateVersionAsync(string versionKey)
        {
            var version = await _cacheService.GetAsync<string>(versionKey);
            if (!string.IsNullOrWhiteSpace(version))
            {
                return version;
            }

            version = Guid.NewGuid().ToString("N");
            await _cacheService.SetAsync(versionKey, version, CarCountCacheTtl);
            return version;
        }

        private static string HashValue(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
