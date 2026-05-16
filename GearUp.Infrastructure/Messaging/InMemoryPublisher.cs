using System.Text.Json;
using GearUp.Application.Common;
using GearUp.Application.Interfaces;
using GearUp.Application.Interfaces.Messaging;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using GearUp.Application.Messaging.Contracts;
using GearUp.Application.ServiceDtos;
using GearUp.Application.ServiceDtos.Message;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GearUp.Infrastructure.Messaging
{
    public sealed class InMemoryPublisher : IMessagePublisher
    {
        private readonly ILogger<InMemoryPublisher> _logger;
        private readonly IServiceProvider _serviceProvider;

        public InMemoryPublisher(ILogger<InMemoryPublisher> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task PublishAsync<TMessage>(TMessage message, string routingKey, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            _logger.LogInformation("[IN-MEMORY QUEUE] Processing message of type {MessageType} synchronously", typeof(TMessage).Name);

            try
            {
                if (message is EmailRequestMessage emailMessage)
                {
                    await HandleEmail(emailMessage);
                }
                else if (message is ImageProcessingMessage imageProcessingMessage)
                {
                    await ProcessImage(imageProcessingMessage);
                }
                else if (message is ImageUploadMessage imageUploadMessage)
                {
                    await UploadImage(imageUploadMessage);
                }
                else if (message is NotificationRequestMessage notificationMessage)
                {
                    await HandleNotification(notificationMessage);
                }
                else
                {
                    _logger.LogWarning("No in-memory handler for message type {MessageType}", typeof(TMessage).Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[IN-MEMORY QUEUE] Error processing message {MessageType}", typeof(TMessage).Name);
                // Synchronous fallback - we may want to throw or just log. We will log to prevent crashing the caller.
            }
        }

        private async Task HandleEmail(EmailRequestMessage message)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            switch (message.TemplateName)
            {
                case "VerifyEmail":
                    await emailSender.SendVerificationEmail(message.ToEmail, message.Payload["token"]);
                    break;

                case "ResetPassword":
                    await emailSender.SendPasswordResetEmail(message.ToEmail, message.Payload["token"]);
                    break;

                case "ResetEmail":
                    await emailSender.SendEmailReset(message.ToEmail, message.Payload["token"]);
                    break;

                default:
                    _logger.LogWarning("Unknown email template: {TemplateName}", message.TemplateName);
                    break;
            }
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

            if (File.Exists(carImage.LocalFilePath)) File.Delete(carImage.LocalFilePath);

            carImage.SetLocalFilePath(processedPath);
            carImage.SetStatus(ImageProcessingStatus.Uploading);
            await commonRepo.SaveChangesAsync();

            // Fire next step in pipeline synchronously
            await publisher.PublishAsync(new ImageUploadMessage
            {
                CarImageId = message.CarImageId,
                CarId = message.CarId,
                DealerId = message.DealerId,
                CorrelationId = message.CorrelationId
            }, "image-upload-queue");
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
            carImage.SetLocalFilePath(null);
            await commonRepo.SaveChangesAsync();

            if (carImage.LocalFilePath != null && File.Exists(carImage.LocalFilePath)) 
            {
                File.Delete(carImage.LocalFilePath);
            }
            
            _logger.LogInformation("Image {CarImageId} uploaded successfully to {Url}", carImage.Id, carImage.Url);
        }

        private async Task HandleNotification(NotificationRequestMessage message)
        {
            using var scope = _serviceProvider.CreateScope();
            var realTimeNotifier = scope.ServiceProvider.GetRequiredService<IRealTimeNotifier>();

            switch (message.MethodName)
            {
                case nameof(IRealTimeNotifier.BroadCastComments):
                    await realTimeNotifier.BroadCastComments(
                        GetGuid(message.Payload, "postId"), 
                        DeserializePayload<CommentDto>(message.Payload["comment"]));
                    break;

                case nameof(IRealTimeNotifier.BroadCastCommentLikes):
                    await realTimeNotifier.BroadCastCommentLikes(
                        GetGuid(message.Payload, "postId"), 
                        GetGuid(message.Payload, "commentId"), 
                        GetInt(message.Payload, "likeCount"));
                    break;

                case nameof(IRealTimeNotifier.BroadCastPostLikes):
                    await realTimeNotifier.BroadCastPostLikes(
                        GetGuid(message.Payload, "postId"), 
                        GetInt(message.Payload, "likeCount"));
                    break;

                case nameof(IRealTimeNotifier.PushNotification):
                    await realTimeNotifier.PushNotification(
                        GetGuid(message.Payload, "receiverId"), 
                        DeserializePayload<NotificationDto>(message.Payload["notification"]));
                    break;

                case nameof(IRealTimeNotifier.SendMessageToUser):
                    await realTimeNotifier.SendMessageToUser(
                        GetGuid(message.Payload, "receiverId"), 
                        DeserializePayload<MessageResponseDto>(message.Payload["message"]));
                    break;

                case nameof(IRealTimeNotifier.SendMessageToConversation):
                    await realTimeNotifier.SendMessageToConversation(
                        GetGuid(message.Payload, "conversationId"), 
                        GetGuid(message.Payload, "excludeUserId"), 
                        DeserializePayload<MessageResponseDto>(message.Payload["message"]));
                    break;

                case nameof(IRealTimeNotifier.NotifyMessageEdited):
                    await realTimeNotifier.NotifyMessageEdited(
                        GetGuid(message.Payload, "conversationId"), 
                        GetGuid(message.Payload, "messageId"), 
                        message.Payload["newText"].ToString()!, 
                        DateTime.Parse(message.Payload["editedAt"].ToString()!));
                    break;

                case nameof(IRealTimeNotifier.NotifyMessageDeleted):
                    await realTimeNotifier.NotifyMessageDeleted(
                        GetGuid(message.Payload, "conversationId"), 
                        GetGuid(message.Payload, "messageId"));
                    break;

                default:
                    _logger.LogWarning("Unknown notification method: {MethodName}", message.MethodName);
                    break;
            }
        }

        private static MemoryStream ConvertToMemoryStream(Stream stream)
        {
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;
            return ms;
        }

        private static Guid GetGuid(Dictionary<string, object> payload, string key)
        {
            return Guid.Parse(payload[key].ToString()!);
        }

        private static int GetInt(Dictionary<string, object> payload, string key)
        {
            return int.Parse(payload[key].ToString()!);
        }

        private static T DeserializePayload<T>(object payload)
        {
            if (payload is JsonElement element)
            {
                return JsonSerializer.Deserialize<T>(element.GetRawText(), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!;
            }
            return (T)payload;
        }
    }
}
