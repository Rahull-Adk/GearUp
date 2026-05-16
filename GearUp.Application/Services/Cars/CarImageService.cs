using GearUp.Application.Common;
using GearUp.Application.Interfaces.Messaging;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.CarServiceInterface;
using GearUp.Application.Messaging.Contracts;
using GearUp.Domain.Entities.Cars;
using GearUp.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GearUp.Application.Services.Cars
{
    public sealed class CarImageService : ICarImageService
    {
        private readonly IMessagePublisher _publisher;
        private readonly ILogger<CarImageService> _logger;

        public CarImageService(IMessagePublisher publisher, ILogger<CarImageService> logger)
        {
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<Result<List<CarImage>>> ProcessForCreateAsync(ICollection<IFormFile> files, Guid dealerId, Guid carId)
        {
            try
            {
                var images = new List<CarImage>();
                var tempDir = Path.Combine(Path.GetTempPath(), "gearup", "raw");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

                foreach (var file in files)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var localPath = Path.Combine(tempDir, fileName);

                    using (var stream = new FileStream(localPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var carImage = CarImage.CreateCarImage(carId, string.Empty, ImageProcessingStatus.Pending, localPath);
                    images.Add(carImage);
                }

                return Result<List<CarImage>>.Success(images, "Images prepared for processing", 202);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to prepare car images for car {CarId}", carId);
                return Result<List<CarImage>>.Failure("Failed to initiate image processing. Please try again.", 500);
            }
        }

        public async Task<Result<List<CarImage>>> ProcessForUpdateAsync(Car existingCar, ICollection<IFormFile>? files, Guid dealerId)
        {
            if (files == null || files.Count == 0)
                return Result<List<CarImage>>.Success(new List<CarImage>(), "No new images", 200);

            try
            {
                var images = new List<CarImage>();
                var tempDir = Path.Combine(Path.GetTempPath(), "gearup", "raw");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

                foreach (var file in files)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var localPath = Path.Combine(tempDir, fileName);

                    using (var stream = new FileStream(localPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var carImage = CarImage.CreateCarImage(existingCar.Id, string.Empty, ImageProcessingStatus.Pending, localPath);
                    images.Add(carImage);
                }

                return Result<List<CarImage>>.Success(images, "New images prepared for processing", 202);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to prepare car images for update for car {CarId}", existingCar.Id);
                return Result<List<CarImage>>.Failure("Failed to initiate image update. Please try again.", 500);
            }
        }

        public async Task PublishImageProcessingMessagesAsync(List<CarImage> images, Guid dealerId, Guid carId)
        {
            foreach (var img in images)
            {
                await _publisher.PublishAsync(new ImageProcessingMessage
                {
                    CarImageId = img.Id,
                    CarId = carId,
                    DealerId = dealerId
                }, "gearup.image.processing.queue");
            }
        }
    }
}
