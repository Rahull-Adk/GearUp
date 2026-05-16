using GearUp.Application.Interfaces.Messaging;
using GearUp.Application.Messaging.Contracts;
using GearUp.Application.Services.Cars;
using GearUp.Domain.Entities.Cars;
using GearUp.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace GearUp.UnitTests.Application.Cars
{
    public class CarImageServiceTests
    {
        private readonly Mock<IMessagePublisher> _publisher = new();
        private readonly Mock<ILogger<CarImageService>> _logger = new();

        private CarImageService CreateService() => new(
            _publisher.Object,
            _logger.Object
        );

        [Fact]
        public async Task ProcessForCreate_PreparesImages()
        {
            // Arrange
            var dealerId = Guid.NewGuid();
            var carId = Guid.NewGuid();
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("test.jpg");
            fileMock.Setup(f => f.Length).Returns(100);
            var files = new List<IFormFile> { fileMock.Object };

            var service = CreateService();

            // Act
            var result = await service.ProcessForCreateAsync(files, dealerId, carId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(202, result.Status);
            Assert.Single(result.Data);
            Assert.Equal(ImageProcessingStatus.Pending, result.Data[0].Status);
            Assert.NotNull(result.Data[0].LocalFilePath);
        }

        [Fact]
        public async Task PublishImageProcessingMessages_PublishesMessages()
        {
            // Arrange
            var dealerId = Guid.NewGuid();
            var carId = Guid.NewGuid();
            var carImage = CarImage.CreateCarImage(carId, "test.jpg", ImageProcessingStatus.Pending, "local.jpg");
            var images = new List<CarImage> { carImage };

            var service = CreateService();

            // Act
            await service.PublishImageProcessingMessagesAsync(images, dealerId, carId);

            // Assert
            _publisher.Verify(p => p.PublishAsync(
                It.Is<ImageProcessingMessage>(m => m.CarId == carId && m.DealerId == dealerId && m.CarImageId == carImage.Id),
                "gearup.image.processing.queue",
                default), Times.Once);
        }
    }
}
