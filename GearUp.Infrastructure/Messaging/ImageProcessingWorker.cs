using System.Text.Json;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Messaging;
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
    public class ImageProcessingWorker : BackgroundService
    {
        private readonly IChannel _channel;
        private readonly ILogger<ImageProcessingWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitMqOptions _rabbitMqOptions;

        public ImageProcessingWorker(IChannel channel, ILogger<ImageProcessingWorker> logger, IServiceProvider serviceProvider, RabbitMqOptions rabbitMqOptions)
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
                var message = JsonSerializer.Deserialize<ImageProcessingMessage>(body);

                try
                {
                    _logger.LogInformation("Processing image {CarImageId} for car {CarId}", message?.CarImageId, message?.CarId);

                    await ProcessImage(message!);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                }
                catch (Exception e)
                {
                    await RabbitMqRetryHelper.HandleMessageFailureAsync(_channel, ea, _rabbitMqOptions, _logger, e, stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(_rabbitMqOptions.ImageProcessingQueue, false, consumer, cancellationToken: stoppingToken);
        }

        private async Task ProcessImage(ImageProcessingMessage message)
        {
            using var scope = _serviceProvider.CreateScope();
            var carRepo = scope.ServiceProvider.GetRequiredService<ICarRepository>();
            var commonRepo = scope.ServiceProvider.GetRequiredService<ICommonRepository>();
            var docProcessor = scope.ServiceProvider.GetRequiredService<IDocumentProcessor>();
            var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

            var car = await carRepo.GetCarEntityByIdAsync(message.CarId);
            var carImage = car?.Images.FirstOrDefault(i => i.Id == message.CarImageId);

            if (carImage == null || string.IsNullOrEmpty(carImage.LocalFilePath))
            {
                _logger.LogWarning("CarImage {CarImageId} not found or has no local path", message.CarImageId);
                return;
            }

            if (carImage.Status != ImageProcessingStatus.Pending)
            {
                _logger.LogInformation("CarImage {CarImageId} is already being processed (Status: {Status})", message.CarImageId, carImage.Status);
                return;
            }

            carImage.SetStatus(ImageProcessingStatus.Processing);
            await commonRepo.SaveChangesAsync();

            Result<MemoryStream> processedResult;
            using (var stream = new FileStream(carImage.LocalFilePath, FileMode.Open, FileAccess.Read))
            {
                // Car listings usually use 800x600 or similar
                processedResult = await docProcessor.ProcessImageFromStream(stream, carImage.LocalFilePath, 800, 600, false);
            }

            if (!processedResult.IsSuccess)
            {
                carImage.SetError(processedResult.ErrorMessage);
                await commonRepo.SaveChangesAsync();
                return;
            }

            var processedDir = Path.Combine(Path.GetTempPath(), "gearup", "processed");
            if (!Directory.Exists(processedDir)) Directory.CreateDirectory(processedDir);

            var processedPath = Path.Combine(processedDir, Path.GetFileName(carImage.LocalFilePath));
            using (var outputStream = new FileStream(processedPath, FileMode.Create))
            {
                await processedResult.Data.CopyToAsync(outputStream);
            }

            // Cleanup raw file
            if (File.Exists(carImage.LocalFilePath)) File.Delete(carImage.LocalFilePath);

            carImage.SetLocalFilePath(processedPath);
            carImage.SetStatus(ImageProcessingStatus.Uploading);
            await commonRepo.SaveChangesAsync();

            await publisher.PublishAsync(new ImageUploadMessage
            {
                CarImageId = message.CarImageId,
                CarId = message.CarId,
                DealerId = message.DealerId,
                CorrelationId = message.CorrelationId
            }, _rabbitMqOptions.ImageUploadQueue);
        }
    }
}
