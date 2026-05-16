using FluentValidation;
using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.CarServiceInterface;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Domain.Entities.Cars;
using GearUp.Domain.Enums;
using GearUp.Domain.Exceptions;
using Microsoft.Extensions.Logging;
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
            IValidator<UpdateCarDto> updateCarValidator,
            IUserRepository userRepository,
            ICacheService cacheService)
        {
            _createCarValidator = createCarValidator;
            _logger = logger;
            _carRepository = carRepository;
            _commonRepository = commonRepository;
            _carImageService = carImageService;
            _updateCarValidator = updateCarValidator;
            _userRepository = userRepository;
            _cacheService = cacheService;
        }

        public async Task<Result<CarResponseDto>> CreateCarAsync(CreateCarRequestDto request, Guid dealerId)
        {
            _logger.LogInformation("Car creation initiated for dealer ID: {DealerId}", dealerId);

            if (dealerId == Guid.Empty)
                throw new Domain.Exceptions.ValidationException("Invalid dealer ID.");

            await _createCarValidator.EnsureValidAsync(request);

            var dealerExist = await _userRepository.UserExistAsync(dealerId);
            if (!dealerExist)
                throw new NotFoundException("Dealer", dealerId);

            if (request.CarImages == null || request.CarImages.Count == 0)
                throw new Domain.Exceptions.ValidationException("At least one car image is required.");

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
                request.LicensePlate,
                validationStatus: CarValidationStatus.Pending,
                status: CarStatus.Available
            );

            await _carRepository.AddCarAsync(newCar);
            await _commonRepository.SaveChangesAsync();
            await InvalidateCarListCacheAsync();
            _logger.LogInformation("Car created successfully for dealer ID: {DealerId}", dealerId);

            return Result<CarResponseDto>.Success(null!, "Car added successfully.", 201);
        }

        public async Task<Result<CursorPageResult<CarListDto>>> GetAllCarsAsync(string? cursorString, CancellationToken cancellationToken = default)
        {
            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    throw new Domain.Exceptions.ValidationException("Invalid cursor");
                }
            }

            var cacheKey = await BuildCarCacheKeyAsync("all", Guid.Empty, cursorString);
            var cachedCars = await _cacheService.GetAsync<CursorPageResult<CarListDto>>(cacheKey);
            if (cachedCars != null)
            {
                return Result<CursorPageResult<CarListDto>>.Success(cachedCars, "Cars fetched successfully", 200);
            }

            var cars = await _carRepository.GetAllCarsAsync(cursor, cancellationToken);
            await _cacheService.SetAsync(cacheKey, cars, CarListCacheTtl);

            return Result<CursorPageResult<CarListDto>>.Success(cars, "Cars fetched successfully", 200);
        }

        public async Task<Result<CarResponseDto>> GetCarByIdAsync(Guid carId, CancellationToken cancellationToken = default)
        {
            var car = await _carRepository.GetCarByIdAsync(carId, cancellationToken)
                      ?? throw new NotFoundException("Car", carId);

            return Result<CarResponseDto>.Success(car, "Car fetched successfully", 200);
        }

        public async Task<Result<CarResponseDto>> UpdateCarAsync(Guid carId, UpdateCarDto request, Guid dealerId)
        {
            _logger.LogInformation("Car Update initiated for car ID: {CarId} by dealer ID: {DealerId}", carId, dealerId);

            var existingCar = await _carRepository.GetCarEntityByIdAsync(carId)
                              ?? throw new NotFoundException("Car", carId);

            if (existingCar.DealerId != dealerId)
                throw new ForbiddenException("Unauthorized to update this car");

            await _updateCarValidator.EnsureValidAsync(request);

            var imagesResult = await _carImageService.ProcessForUpdateAsync(existingCar, request.CarImages, dealerId);
            if (!imagesResult.IsSuccess)
                return Result<CarResponseDto>.Failure(imagesResult.ErrorMessage, imagesResult.Status);

            existingCar.UpdateDetails(
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

            var existingCar = await _carRepository.GetCarEntityByIdAsync(carId)
                              ?? throw new NotFoundException("Car", carId);

            var user = await _userRepository.GetUserByIdAsync(dealerId)
                       ?? throw new NotFoundException("Dealer", dealerId);

            if (existingCar.DealerId != dealerId)
                throw new ForbiddenException("Unauthorized to delete this car");

            existingCar.DeleteCar();
            await _commonRepository.SaveChangesAsync();
            await InvalidateCarListCacheAsync();

            _logger.LogInformation("Car deleted successfully for car ID: {CarId}", carId);
            return Result<string>.Success(default!, "Car deleted successfully", 200);
        }
        public async Task<Result<CursorPageResult<CarListDto>>> GetDealerCarsAsync(Guid dealerId, string? cursorString, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting cars of {DealerId}", dealerId);

            var dealer = await _userRepository.GetUserByIdAsync(dealerId)
                         ?? throw new NotFoundException("Dealer", dealerId);

            if (dealer.Role != UserRole.Dealer)
                throw new ForbiddenException("Cars are only available for dealer accounts.");

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    throw new Domain.Exceptions.ValidationException("Invalid cursor");
                }
            }

            var cars = await _carRepository.GetDealerCarsAsync(dealerId, cursor, cancellationToken);

            return Result<CursorPageResult<CarListDto>>.Success(cars, "Cars fetched successfully");
        }

        public async Task<Result<CursorPageResult<CarListDto>>> SearchCarsAsync(CarSearchDto? searchDto, string? cursorString, CancellationToken cancellationToken = default)
        {
            if (searchDto == null)
                throw new Domain.Exceptions.ValidationException("Search criteria cannot be null");

            if(searchDto.Query == null && searchDto.Color == null && searchDto.MinPrice == null && searchDto.MaxPrice == null)
                throw new Domain.Exceptions.ValidationException("At least one search criteria must be provided");

            if(searchDto.MinPrice != null && searchDto.MaxPrice != null && searchDto.MinPrice > searchDto.MaxPrice)
                throw new Domain.Exceptions.ValidationException("MinPrice cannot be greater than MaxPrice");

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    throw new Domain.Exceptions.ValidationException("Invalid cursor");
                }
            }

            var cacheKey = await BuildCarCacheKeyAsync("search", Guid.Empty, cursorString, searchDto);
            var cachedCars = await _cacheService.GetAsync<CursorPageResult<CarListDto>>(cacheKey);
            if (cachedCars != null)
            {
                return Result<CursorPageResult<CarListDto>>.Success(cachedCars, "Cars fetched successfully", 200);
            }

            var cars = await _carRepository.SearchCarsAsync(searchDto, cursor, cancellationToken);
            await _cacheService.SetAsync(cacheKey, cars, CarListCacheTtl);
            return Result<CursorPageResult<CarListDto>>.Success(cars, "Cars fetched successfully", 200);
        }

        public async Task<Result<CursorPageResult<CarListDto>>> GetMyCarsAsync(Guid dealerId, CarValidationStatus status, string? cursorString, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting cars of {DealerId}", dealerId);

            var dealerExists = await _userRepository.UserExistAsync(dealerId);
            if (!dealerExists)
                throw new NotFoundException("Dealer", dealerId);

            if (status != CarValidationStatus.Approved && status != CarValidationStatus.Pending &&
                status != CarValidationStatus.Rejected)
            {
                throw new Domain.Exceptions.ValidationException("Invalid car status");
            }

            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(cursorString))
            {
                if (!Cursor.TryDecode(cursorString, out cursor))
                {
                    throw new Domain.Exceptions.ValidationException("Invalid cursor");
                }
            }

            var cacheKey = await BuildCarCacheKeyAsync("my", dealerId, cursorString, status.ToString());
            var cachedCars = await _cacheService.GetAsync<CursorPageResult<CarListDto>>(cacheKey);
            if (cachedCars != null)
            {
                return Result<CursorPageResult<CarListDto>>.Success(cachedCars, $"{status} cars fetched successfully");
            }

            var cars = await _carRepository.GetMyCarsAsync(dealerId, status, cursor, cancellationToken);
            await _cacheService.SetAsync(cacheKey, cars, CarListCacheTtl);

            return Result<CursorPageResult<CarListDto>>.Success(cars, $"{status} cars fetched successfully");
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
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value));
                return Convert.ToHexString(bytes).ToLowerInvariant();
            }
        }
    }
}
