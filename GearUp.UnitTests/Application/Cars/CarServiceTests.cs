using AutoMapper;
using FluentValidation;
using FluentValidation.Results
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.CarServiceInterface;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Application.Services.Cars;
using GearUp.Domain.Entities.Cars;
using GearUp.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;


namespace GearUp.UnitTests.Application.Cars
{
    public class CarServiceTests
    {
        private readonly Mock<IValidator<CreateCarRequestDto>> _createValidator = new();
        private readonly Mock<IValidator<UpdateCarDto>> _updateValidator = new();
        private readonly Mock<ICacheService> _cache = new();
        private readonly Mock<ILogger<CarService>> _logger = new();
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<ICarRepository> _carRepo = new();
        private readonly Mock<ICloudinaryImageUploader> _uploader = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ICommonRepository> _commonRepo = new();
        private readonly Mock<IDocumentProcessor> _docProcessor = new();
        private readonly Mock<ICarImageService> _carImageService = new();

        private CarService CreateService() => new(
        _createValidator.Object,
        _cache.Object,
        _logger.Object,
        _carRepo.Object,
        _mapper.Object,
        _commonRepo.Object,
        _carImageService.Object,
        _updateValidator.Object
            );

        private static ValidationResult Valid() => new ValidationResult();
        private static ValidationResult Invalid(params string[] messages)
        => new ValidationResult(messages.Select(m => new ValidationFailure("field", m)).ToList());

        [Fact]
        public async Task CreateCar_InvalidDealerId_Returns400()
        {
            var service = CreateService();
            var req = new CreateCarRequestDto { Title = "t" };

            var result = await service.CreateCarAsync(req, Guid.Empty);

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.Status);
        }

        [Fact]
        public async Task CreateCar_ValidationFails_Returns400()
        {
            var service = CreateService();
            var req = new CreateCarRequestDto { Title = "t" };
            _createValidator.Setup(v => v.Validate(req)).Returns(Invalid("bad title"));

            var result = await service.CreateCarAsync(req, Guid.NewGuid());

            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.Status);
            _createValidator.Verify(v => v.Validate(req), Times.Once);
        }

        [Fact]
        public async Task CreateCar_NoImages_Returns422()
        {
            var service = CreateService();
            var req = new CreateCarRequestDto { Title = "t", CarImages = new List<IFormFile>() };
            _createValidator.Setup(v => v.Validate(req)).Returns(Valid());

            var result = await service.CreateCarAsync(req, Guid.NewGuid());

            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.Status);
        }

        [Fact]
        public async Task GetAllCars_NoCars_Returns200WithMessage()
        {
            var service = CreateService();
            _carRepo.Setup(r => r.GetAllCarsAsync()).ReturnsAsync(new List<Car>());
            var result = await service.GetAllCarsAsync();
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.Status);
            Assert.Null(result.Data);
            Assert.Equal("No cars found", result.SuccessMessage);
        }

        [Fact]
        public async Task GetAllCars_MapsAndCaches()
        {
            var service = CreateService();
            var dealerId = Guid.NewGuid();
            var car = Car.CreateForSale(
                Guid.NewGuid(),
            title: "Toyota Camry",
            description: "desc",
            model: "Camry",
            make: "Toyota",
            year: 2020,
            price: 20000,
            color: "Black",
            mileage: 10000,
            seatingCapacity: 5,
            engineCapacity: 2500,
            imageUrls: new List<CarImage>(),
            fuelType: FuelType.Petrol,
            condition: CarCondition.Used,
            transmission: TransmissionType.Automatic,
            dealerId: dealerId,
            vin: "VIN1",
            licensePlate: "ABC123");
            _carRepo.Setup(r => r.GetAllCarsAsync()).ReturnsAsync(new List<Car> { car });

            ICollection<CreateCarResponseDto> mapped = new List<CreateCarResponseDto>
            {
                new() { Id = car.Id, Title = car.Title }
            };


            _mapper.Setup(m => m.Map<ICollection<CreateCarResponseDto>>(It.IsAny<List<Car>>()))
            .Returns(mapped);

            var result = await service.GetAllCarsAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.Status);
            Assert.Single(result.Data!);
            _cache.Verify(c =>
                c.SetAsync(
                    "cars:all",
                mapped,
                    It.IsAny<TimeSpan>()),
                Times.Once);

        }

        [Fact]
        public async Task GetCarById_FromCache_ReturnsCached()
        {
            var service = CreateService();
            var id = Guid.NewGuid();
            var cached = new CreateCarResponseDto { Id = id, Title = "cached" };
            _cache.Setup(c => c.GetAsync<CreateCarResponseDto>($"car:{id}")).ReturnsAsync(cached);

            var result = await service.GetCarByIdAsync(id);

            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.Status);
            Assert.Equal("cached", result.Data.Title);
            _carRepo.Verify(r => r.GetCarByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetCarById_NotFound_Returns404()
        {
            var service = CreateService();
            var id = Guid.NewGuid();
            _cache.Setup(c => c.GetAsync<CreateCarResponseDto>($"car:{id}")).ReturnsAsync((CreateCarResponseDto?)null);
            _carRepo.Setup(r => r.GetCarByIdAsync(id)).ReturnsAsync((Car?)null);

            var result = await service.GetCarByIdAsync(id);

            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.Status);
        }

        [Fact]
        public async Task UpdateCar_CarNotFound_Returns404()
        {
            var service = CreateService();
            var carId = Guid.NewGuid();
            _carRepo.Setup(r => r.GetCarByIdAsync(carId)).ReturnsAsync((Car?)null);

            var result = await service.UpdateCarAsync(carId, new UpdateCarDto(), Guid.NewGuid());

            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.Status);
        }

        [Fact]
        public async Task UpdateCar_UnauthorizedDealer_Returns403()
        {
            var service = CreateService();
            var dealerId = Guid.NewGuid();
            var otherDealer = Guid.NewGuid();
            var car = Car.CreateForSale(Guid.NewGuid(), "t", "d", "m", "mk", 2020, 1000, "c", 10, 4, 2000, new List<CarImage>(), FuelType.Petrol, CarCondition.New, TransmissionType.Automatic, otherDealer, "VIN", "PLT");
            _carRepo.Setup(r => r.GetCarByIdAsync(car.Id)).ReturnsAsync(car);

            var result = await service.UpdateCarAsync(car.Id, new UpdateCarDto(), dealerId);

            Assert.False(result.IsSuccess);
            Assert.Equal(403, result.Status);
        }
    }
}
