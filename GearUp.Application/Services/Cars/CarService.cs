using AutoMapper;
using FluentValidation;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.CarServiceInterface;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Domain.Entities.Cars;
using Microsoft.Extensions.Logging;

namespace GearUp.Application.Services.Cars
{
    public class CarService : ICarService
    {
        private readonly IValidator<CreateCarRequestDto> _createCarValidator;
        private readonly IValidator<UpdateCarDto> _updateCarValidator;
        private readonly ICacheService _cache;
        private readonly ILogger<CarService> _logger;
        private readonly ICarRepository _carRepository;
        private readonly IMapper _mapper;
        private readonly ICommonRepository _commonRepository;
        private readonly ICarImageService _carImageService;

        public CarService(
            IValidator<CreateCarRequestDto> createCarValidator,
            ICacheService cache,
            ILogger<CarService> logger,
            ICarRepository carRepository,
            IMapper mapper,
            ICommonRepository commonRepository,
            ICarImageService carImageService, IValidator<UpdateCarDto> updateCarDtoValiator)
        {
            _createCarValidator = createCarValidator;
            _cache = cache;
            _logger = logger;
            _mapper = mapper;
            _carRepository = carRepository;
            _commonRepository = commonRepository;
            _carImageService = carImageService;
            _updateCarValidator = updateCarDtoValiator;
        }

        public async Task<Result<CreateCarResponseDto>> CreateCarAsync(CreateCarRequestDto request, Guid dealerId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Car creation initiated for dealer ID: {DealerId}", dealerId);

            if (dealerId == Guid.Empty)
                return Result<CreateCarResponseDto>.Failure("Invalid dealer ID.", 400);

            var validationResult = _createCarValidator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Car creation failed validation for dealer ID: {DealerId}. Errors: {Errors}", dealerId, errors);
                return Result<CreateCarResponseDto>.Failure(errors, 422);
            }

            if (request.CarImages == null || request.CarImages.Count == 0)
            {
                _logger.LogWarning("Car creation failed: No images provided for dealer ID: {DealerId}", dealerId);
                return Result<CreateCarResponseDto>.Failure("At least one car image is required.", 422);
            }

            if (await _carRepository.IsUniqueVin(request.VIN))
            {
                _logger.LogWarning("Car creation failed: VIN {VIN} already exists for dealer ID: {DealerId}", request.VIN, dealerId);
                return Result<CreateCarResponseDto>.Failure("A car with the provided VIN already exists.", 409);
            }
            Guid carId = Guid.NewGuid();
            var imagesResult = await _carImageService.ProcessForCreateAsync(request.CarImages, dealerId, carId, cancellationToken);
            if (!imagesResult.IsSuccess)
            {
                return Result<CreateCarResponseDto>.Failure(imagesResult.ErrorMessage, imagesResult.Status);
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
            _logger.LogInformation("Car created successfully for dealer ID: {DealerId}", dealerId);

            var cacheKey = $"car:{newCar.Id}";
            await _cache.SetAsync(cacheKey, newCar, TimeSpan.FromHours(1));
            var responseData = _mapper.Map<CreateCarResponseDto>(newCar);
            return Result<CreateCarResponseDto>.Success(responseData, "Car added successfully.", 201);
        }

        public async Task<Result<ICollection<CreateCarResponseDto>>> GetAllCarsAsync(CancellationToken cancellationToken = default)
        {
            var cachedKey = "cars:all";
            var cachedCars = await _cache.GetAsync<ICollection<CreateCarResponseDto>>(cachedKey);
            if (cachedCars != null)
            {
                _logger.LogInformation("Fetched all cars from cache.");
                return Result<ICollection<CreateCarResponseDto>>.Success(cachedCars, "Cars fetched successfully", 200);
            }

            var cars = await _carRepository.GetAllCarsAsync();
            if (cars.Count == 0)
            {
                return Result<ICollection<CreateCarResponseDto>>.Success(null!, "No cars found", 200);
            }
            var responseData = _mapper.Map<ICollection<CreateCarResponseDto>>(cars);
            await _cache.SetAsync(cachedKey, responseData, TimeSpan.FromHours(1));
            return Result<ICollection<CreateCarResponseDto>>.Success(responseData, "Cars fetched successfully", 200);
        }

        public async Task<Result<CreateCarResponseDto>> GetCarByIdAsync(Guid carId, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"car:{carId}";
            var cachedCar = await _cache.GetAsync<CreateCarResponseDto>(cacheKey);
            if (cachedCar != null)
            {
                _logger.LogInformation("Fetched car ID: {CarId} from cache.", carId);
                return Result<CreateCarResponseDto>.Success(cachedCar, "Car fetched successfully", 200);
            }
            var car = await _carRepository.GetCarByIdAsync(carId);
            if (car == null)
            {
                _logger.LogWarning("Car ID: {CarId} not found.", carId);
                return Result<CreateCarResponseDto>.Failure("Car not found", 404);
            }
            var responseData = _mapper.Map<CreateCarResponseDto>(car);
            await _cache.SetAsync(cacheKey, responseData, TimeSpan.FromHours(1));
            return Result<CreateCarResponseDto>.Success(responseData, "Car fetched successfully", 200);
        }

        public async Task<Result<CreateCarResponseDto>> UpdateCarAsync(Guid carId, UpdateCarDto request, Guid dealerId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Car Update initiated for car ID: {CarId} by dealer ID: {DealerId}", carId, dealerId);

            var existingCar = await _carRepository.GetCarByIdAsync(carId);
            if (existingCar is null)
            {
                _logger.LogWarning("No existing car found for car ID: {CarId}", carId);
                return Result<CreateCarResponseDto>.Failure("Car not found", 404);
            }
            var validation = ValidateCarUpdate(existingCar, request, dealerId);
            if (!validation.IsSuccess)
                return validation;

            var imagesResult = await _carImageService.ProcessForUpdateAsync(existingCar!, request.CarImages, dealerId, cancellationToken);
            if (!imagesResult.IsSuccess)
                return Result<CreateCarResponseDto>.Failure(imagesResult.ErrorMessage, imagesResult.Status);

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
            _logger.LogInformation("Car updated successfully for car ID: {CarId}", carId);

            await _cache.SetAsync($"car:{existingCar.Id}", existingCar, TimeSpan.FromHours(1));
            var response = _mapper.Map<CreateCarResponseDto>(existingCar);

            return Result<CreateCarResponseDto>.Success(response, "Car updated successfully", 200);
        }

        public Task<Result<CreateCarResponseDto>> CreateCarAsync(CreateCarRequestDto request, Guid dealerId)
 => CreateCarAsync(request, dealerId, default);

        public Task<Result<ICollection<CreateCarResponseDto>>> GetAllCarsAsync()
 => GetAllCarsAsync(default);

        public Task<Result<CreateCarResponseDto>> GetCarByIdAsync(Guid carId)
 => GetCarByIdAsync(carId, default);

        public Task<Result<CreateCarResponseDto>> UpdateCarAsync(Guid carId, UpdateCarDto request, Guid dealerId)
 => UpdateCarAsync(carId, request, dealerId, default);

        private Result<CreateCarResponseDto> ValidateCarUpdate(Car? existingCar, UpdateCarDto request, Guid dealerId)
        {
            if (existingCar == null)
                return Result<CreateCarResponseDto>.Failure("Car not found", 404);

            if (existingCar.DealerId != dealerId)
                return Result<CreateCarResponseDto>.Failure("Unauthorized to update this car", 403);

            var validationResult = _updateCarValidator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<CreateCarResponseDto>.Failure(errors, 422);
            }

            return Result<CreateCarResponseDto>.Success(null!, "Validation passed", 200);
        }
    }
}