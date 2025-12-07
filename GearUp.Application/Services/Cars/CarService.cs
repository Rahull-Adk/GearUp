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
        private readonly IUserRepository _userRepository;

        public CarService(
            IValidator<CreateCarRequestDto> createCarValidator,
            ICacheService cache,
            ILogger<CarService> logger,
            ICarRepository carRepository,
            IMapper mapper,
            ICommonRepository commonRepository,
            ICarImageService carImageService, IValidator<UpdateCarDto> updateCarDtoValiator,
            IUserRepository userRepository)
        {
            _createCarValidator = createCarValidator;
            _cache = cache;
            _logger = logger;
            _mapper = mapper;
            _carRepository = carRepository;
            _commonRepository = commonRepository;
            _carImageService = carImageService;
            _updateCarValidator = updateCarDtoValiator;
            _userRepository = userRepository;
        }

        public async Task<Result<CreateCarResponseDto>> CreateCarAsync(CreateCarRequestDto request, Guid dealerId)
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

            var dealer = await _userRepository.GetUserByIdAsync(dealerId);
            if (dealer == null)
                {
                _logger.LogWarning("Car creation failed: Dealer not found for ID: {DealerId}", dealerId);
                return Result<CreateCarResponseDto>.Failure("Dealer not found.", 404);
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
            var imagesResult = await _carImageService.ProcessForCreateAsync(request.CarImages, dealerId, carId);
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

        public async Task<Result<PageResult<CreateCarResponseDto>>> GetAllCarsAsync(int pageNum)
        {
            if(pageNum < 1)
            {
                return Result<PageResult<CreateCarResponseDto>>.Failure("Page number must be greater than zero", 400);
            }
            var cachedKey = $"cars:all:{pageNum}";
            var cachedCars = await _cache.GetAsync<PageResult<CreateCarResponseDto>>(cachedKey);
            if (cachedCars != null)
            {
                _logger.LogInformation("Fetched all cars from cache.");
                return Result<PageResult<CreateCarResponseDto>>.Success(cachedCars, "Cars fetched successfully", 200);
            }

            var cars = await _carRepository.GetAllCarsAsync(pageNum);

            var dto = new PageResult<CreateCarResponseDto>
            {
                TotalCount = cars.TotalCount,
                CurrentPage = cars.CurrentPage,
                PageSize = cars.PageSize,
                TotalPages = cars.TotalPages,
                Items = _mapper.Map<List<CreateCarResponseDto>>(cars.Items),
            };

            await _cache.SetAsync(cachedKey, dto, TimeSpan.FromHours(1));
            return Result<PageResult<CreateCarResponseDto>>.Success(dto, "Cars fetched successfully", 200);
        }

        public async Task<Result<CreateCarResponseDto>> GetCarByIdAsync(Guid carId)
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

        public async Task<Result<CreateCarResponseDto>> UpdateCarAsync(Guid carId, UpdateCarDto request, Guid dealerId)
        {
            _logger.LogInformation("Car Update initiated for car ID: {CarId} by dealer ID: {DealerId}", carId, dealerId);

            var existingCar = await _carRepository.GetCarByIdAsync(carId);
            if (existingCar is null)
            {
                _logger.LogWarning("No existing car found for car ID: {CarId}", carId);
                return Result<CreateCarResponseDto>.Failure("Car not found", 404);
            }

            if (existingCar.DealerId != dealerId)
            {
                _logger.LogWarning("Unauthorized deletion attempt for car ID: {CarId} by dealer ID: {DealerId}", carId, dealerId);
                return Result<CreateCarResponseDto>.Failure("Unauthorized to delete this car", 403);
            }

            var validation = ValidateCarUpdate(existingCar, request, dealerId);
            if (!validation.IsSuccess)
                return validation;

            var imagesResult = await _carImageService.ProcessForUpdateAsync(existingCar!, request.CarImages, dealerId);
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

        public async Task<Result<string>> DeleteCarByIdAsync(Guid carId, Guid dealerId)
        {
            _logger.LogInformation("Car Deletion initiated for car ID: {CarId} by dealer ID: {DealerId}", carId, dealerId);

            var existingCar = await _carRepository.GetCarByIdAsync(carId);
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

            _logger.LogInformation("Car deleted successfully for car ID: {CarId}", carId);
            await _cache.RemoveAsync($"car:{existingCar.Id}");
            return Result<string>.Success(default!, "Car deleted successfully", 200);

        }

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

        public async Task<Result<PageResult<CreateCarResponseDto>>> SearchCarsAsync(CarSearchDto searchDto)
        {
            if (searchDto == null)
            {
                return Result<PageResult<CreateCarResponseDto>>.Failure("Search criteria cannot be null", 400);
            }

            if(searchDto.Query == null && searchDto.Color == null && searchDto.MinPrice == null && searchDto.MaxPrice == null)
            {
                return Result<PageResult<CreateCarResponseDto>>.Failure("At least one search criteria must be provided", 400);
            }

            if(searchDto.Page < 1)
            {
                return Result<PageResult<CreateCarResponseDto>>.Failure("Page number must be greater than zero", 400);
            }

            if(searchDto.MinPrice != null && searchDto.MaxPrice != null && searchDto.MinPrice > searchDto.MaxPrice)
            {
                return Result<PageResult<CreateCarResponseDto>>.Failure("MinPrice cannot be greater than MaxPrice", 400);
            }

            if(searchDto.SortBy != null)
            {
                var validSortBy = new List<string> { "price", "year", "make", "model" };
                if(!validSortBy.Contains(searchDto.SortBy.ToLower()))
                {
                    return Result<PageResult<CreateCarResponseDto>>.Failure($"Invalid SortBy value. Valid values are: {string.Join(", ", validSortBy)}", 400);
                }
            }

            if(searchDto.SortOrder != null)
            {
                var validSortOrder = new List<string> { "asc", "desc" };
                if(!validSortOrder.Contains(searchDto.SortOrder.ToLower()))
                {
                    return Result<PageResult<CreateCarResponseDto>>.Failure($"Invalid SortOrder value. Valid values are: {string.Join(", ", validSortOrder)}", 400);
                }
            }

            var cacheKey = $"cars:search:{searchDto.Query}:{searchDto.Color}:{searchDto.MinPrice}:{searchDto.MaxPrice}:{searchDto.Page}:{searchDto.SortBy}:{searchDto.SortOrder}";

            var cachedResult = await _cache.GetAsync<PageResult<CreateCarResponseDto>>(cacheKey);

            if (cachedResult is not null)
            {
                _logger.LogInformation("Fetched search results from cache for key: {CacheKey}", cacheKey);
                return Result<PageResult<CreateCarResponseDto>>.Success(cachedResult, "Search results fetched successfully", 200);
            }

            var cars = await _carRepository.SearchCarsAsync(searchDto);

            var dto = new PageResult<CreateCarResponseDto>
            {
                TotalCount = cars.TotalCount,
                CurrentPage = cars.CurrentPage,
                PageSize = cars.PageSize,
                TotalPages = cars.TotalPages,
                Items = _mapper.Map<List<CreateCarResponseDto>>(cars.Items),
            };

            await _cache.SetAsync(cacheKey, dto, TimeSpan.FromHours(1));

            return Result<PageResult<CreateCarResponseDto>>.Success(dto, "Cars fetched successfully", 200);
        }
    }
}