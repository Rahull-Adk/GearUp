using FluentValidation;
using FluentValidation.Results;
using GearUp.Application.Common.Pagination;
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
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<IValidator<UpdateCarDto>> _updateValidator = new();
        private readonly Mock<ILogger<CarService>> _logger = new();
        private readonly Mock<ICarRepository> _carRepo = new();
        private readonly Mock<ICommonRepository> _commonRepo = new();
        private readonly Mock<ICarImageService> _carImageService = new();
        private readonly Mock<ICacheService> _cacheService = new();

        private CarService CreateService() => new(
            _createValidator.Object,
            _logger.Object,
            _carRepo.Object,
            _commonRepo.Object,
            _carImageService.Object,
            _updateValidator.Object,
            _userRepository.Object,
            _cacheService.Object
        );

        private static ValidationResult Valid() => new ValidationResult();
        private static ValidationResult Invalid(params string[] messages)
        => new ValidationResult(messages.Select(m => new ValidationFailure("field", m)).ToList());

        [Fact]
        public async Task CreateCar_InvalidDealerId_Returns400()
        {
            var service = CreateService();
            var req = new CreateCarRequestDto { Title = "t" };
            
            await Assert.ThrowsAsync<GearUp.Domain.Exceptions.ValidationException>(() => 
                service.CreateCarAsync(req, Guid.Empty));
        }

        [Fact]
        public async Task CreateCar_ValidationFails_Returns422()
        {
            var service = CreateService();
            var req = new CreateCarRequestDto { Title = "t" };
            _createValidator.Setup(v => v.Validate(It.IsAny<CreateCarRequestDto>()))
                .Returns(Invalid("bad title"));

            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => 
                service.CreateCarAsync(req, Guid.NewGuid()));
            
            _createValidator.Verify(v => v.Validate(It.IsAny<CreateCarRequestDto>()), Times.Once);
        }

        [Fact]
        public async Task CreateCar_NoImages_Returns422()
        {
            var service = CreateService();
            var req = new CreateCarRequestDto { Title = "t", CarImages = new List<IFormFile>() };
            _userRepository.Setup(u => u.UserExistAsync(It.IsAny<Guid>())).ReturnsAsync(true);
            _createValidator.Setup(v => v.Validate(req)).Returns(Valid());

            await Assert.ThrowsAsync<GearUp.Domain.Exceptions.ValidationException>(() => 
                service.CreateCarAsync(req, Guid.NewGuid()));
        }

        [Fact]
        public async Task GetAllCars_NoCars_ReturnsEmptyItems()
        {
            var service = CreateService();
            var pageResult = new CursorPageResult<CarListDto>
            {
                Items = new List<CarListDto>(),
                HasMore = false,
                NextCursor = null
            };
            _carRepo.Setup(r => r.GetAllCarsAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(pageResult);
            // mapper mapping not used in service (repository returns DTOs), keep for compatibility
            var result = await service.GetAllCarsAsync(null);
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.Status);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data.Items);
        }
        [Fact]
        public async Task GetCarById_NotFound_Returns404()
        {
            var service = CreateService();
            var id = Guid.NewGuid();

            _carRepo.Setup(r => r.GetCarByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((CarResponseDto?)null);

            await Assert.ThrowsAsync<GearUp.Domain.Exceptions.NotFoundException>(() => 
                service.GetCarByIdAsync(id));
        }

        [Fact]
        public async Task UpdateCar_CarNotFound_Returns404()
        {
            var service = CreateService();
            var carId = Guid.NewGuid();
            _carRepo.Setup(r => r.GetCarEntityByIdAsync(carId)).ReturnsAsync((Car?)null);

            await Assert.ThrowsAsync<GearUp.Domain.Exceptions.NotFoundException>(() => 
                service.UpdateCarAsync(carId, new UpdateCarDto(), Guid.NewGuid()));
        }

        [Fact]
        public async Task UpdateCar_UnauthorizedDealer_Returns403()
        {
            var service = CreateService();
            var dealerId = Guid.NewGuid();
            var otherDealer = Guid.NewGuid();
            var car = Car.CreateForSale(Guid.NewGuid(),"t","d","m","mk",2020,1000,"c",10,4,2000,new List<CarImage>(),FuelType.Petrol,CarCondition.New,TransmissionType.Automatic,otherDealer,"VIN","PLT");
            _carRepo.Setup(r => r.GetCarEntityByIdAsync(car.Id)).ReturnsAsync(car);

            await Assert.ThrowsAsync<GearUp.Domain.Exceptions.ForbiddenException>(() => 
                service.UpdateCarAsync(car.Id, new UpdateCarDto(), dealerId));
        }
    }
}
