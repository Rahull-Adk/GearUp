using System.Text.Json;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Messaging.Contracts;
using GearUp.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GearUp.Infrastructure.Messaging
{
    public class ImageUploadWorker : BackgroundService
    {
        private readonly IChannel _channel;
        private readonly ILogger<ImageUploadWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitMqOptions _rabbitMqOptions;

        public ImageUploadWorker(IChannel channel, ILogger<ImageUploadWorker> logger, IServiceProvider serviceProvider, RabbitMqOptions rabbitMqOptions)
        {
            _channel = channel;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _rabbitMqOptions = rabbitMqOptions;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = JsonSerializer.Deserialize<ImageUploadMessage>(body);

                try
                {
                    _logger.LogInformation("Uploading image {CarImageId} for car {CarId}", message?.CarImageId, message?.CarId);

                    await UploadImage(message!);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                }
                catch (Exception e)
                {
                    await RabbitMqRetryHelper.HandleMessageFailureAsync(_channel, ea, _rabbitMqOptions, _logger, e, stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(_rabbitMqOptions.ImageUploadQueue, false, consumer, cancellationToken: stoppingToken);
        }

        private async Task UploadImage(ImageUploadMessage message)
        {
            using var scope = _serviceProvider.CreateScope();
            var carRepo = scope.ServiceProvider.GetRequiredService<ICarRepository>();
            var commonRepo = scope.ServiceProvider.GetRequiredService<ICommonRepository>();
            var uploader = scope.ServiceProvider.GetRequiredService<ICloudinaryImageUploader>();

            var car = await carRepo.GetCarEntityByIdAsync(message.CarId);
            var carImage = car?.Images.FirstOrDefault(i => i.Id == message.CarImageId);

            if (carImage == null || string.IsNullOrEmpty(carImage.LocalFilePath))
            {
                _logger.LogWarning("CarImage {CarImageId} not found or has no local path", message.CarImageId);
                return;
            }

            if (carImage.Status == ImageProcessingStatus.Completed)
            {
                _logger.LogInformation("CarImage {CarImageId} is already uploaded", message.CarImageId);
                return;
            }

            carImage.SetStatus(ImageProcessingStatus.Uploading);
            await commonRepo.SaveChangesAsync();

            List<Uri> uris;
            using (var stream = new FileStream(carImage.LocalFilePath, FileMode.Open, FileAccess.Read))
            {
                var uploadPath = $"gearup/dealers/{message.DealerId}/cars";
                uris = await uploader.UploadImageListAsync(new List<MemoryStream> { ConvertToMemoryStream(stream) }, uploadPath);
            }

            if (uris == null || uris.Count == 0)
            {
                throw new Exception("Cloudinary upload returned no URIs");
            }

            carImage.SetUrl(uris[0].ToString());
            carImage.SetStatus(ImageProcessingStatus.Completed);
            carImage.SetLocalFilePath(null); // Clear local path after success
            await commonRepo.SaveChangesAsync();

            // Cleanup processed file
            if (File.Exists(carImage.LocalFilePath)) File.Delete(carImage.LocalFilePath);
            
            _logger.LogInformation("Image {CarImageId} uploaded successfully to {Url}", carImage.Id, carImage.Url);
        }

        private static MemoryStream ConvertToMemoryStream(Stream stream)
        {
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;
            return ms;
        }
    }
}
