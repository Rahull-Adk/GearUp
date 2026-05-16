using System.Text.Json;
using GearUp.Application.Interfaces;
using GearUp.Application.Messaging.Contracts;
using GearUp.Application.ServiceDtos;
using GearUp.Application.ServiceDtos.Message;
using GearUp.Application.ServiceDtos.Post;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GearUp.Infrastructure.Messaging
{
    public class NotificationConsumerWorker : BackgroundService
    {
        private readonly IChannel _channel;
        private readonly ILogger<NotificationConsumerWorker> _logger;
        private readonly IRealTimeNotifier _realTimeNotifier;
        private readonly RabbitMqOptions _rabbitMqOptions;

        public NotificationConsumerWorker(IChannel channel, ILogger<NotificationConsumerWorker> logger, IRealTimeNotifier realTimeNotifier, RabbitMqOptions rabbitMqOptions)
        {
            _channel = channel;
            _logger = logger;
            _realTimeNotifier = realTimeNotifier;
            _rabbitMqOptions = rabbitMqOptions;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = JsonSerializer.Deserialize<NotificationRequestMessage>(body);

                try
                {
                    _logger.LogInformation("Processing notification {MethodName} - {CorrelationId}", message?.MethodName, message?.CorrelationId);

                    await HandleNotification(message!);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                }
                catch (Exception e)
                {
                    await RabbitMqRetryHelper.HandleMessageFailureAsync(_channel, ea, _rabbitMqOptions, _logger, e, stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(_rabbitMqOptions.NotificationQueue, false, consumer, cancellationToken: stoppingToken);
        }

        private async Task HandleNotification(NotificationRequestMessage message)
        {
            switch (message.MethodName)
            {
                case nameof(IRealTimeNotifier.BroadCastComments):
                    {
                        var postId = GetGuid(message.Payload, "postId");
                        var comment = DeserializePayload<CommentDto>(message.Payload["comment"]);
                        await _realTimeNotifier.BroadCastComments(postId, comment);
                    }
                    break;

                case nameof(IRealTimeNotifier.BroadCastCommentLikes):
                    {
                        var postId = GetGuid(message.Payload, "postId");
                        var commentId = GetGuid(message.Payload, "commentId");
                        var likeCount = GetInt(message.Payload, "likeCount");
                        await _realTimeNotifier.BroadCastCommentLikes(postId, commentId, likeCount);
                    }
                    break;

                case nameof(IRealTimeNotifier.BroadCastPostLikes):
                    {
                        var postId = GetGuid(message.Payload, "postId");
                        var likeCount = GetInt(message.Payload, "likeCount");
                        await _realTimeNotifier.BroadCastPostLikes(postId, likeCount);
                    }
                    break;

                case nameof(IRealTimeNotifier.PushNotification):
                    {
                        var receiverId = GetGuid(message.Payload, "receiverId");
                        var notification = DeserializePayload<NotificationDto>(message.Payload["notification"]);
                        await _realTimeNotifier.PushNotification(receiverId, notification);
                    }
                    break;

                case nameof(IRealTimeNotifier.SendMessageToUser):
                    {
                        var receiverId = GetGuid(message.Payload, "receiverId");
                        var msg = DeserializePayload<MessageResponseDto>(message.Payload["message"]);
                        await _realTimeNotifier.SendMessageToUser(receiverId, msg);
                    }
                    break;

                case nameof(IRealTimeNotifier.SendMessageToConversation):
                    {
                        var conversationId = GetGuid(message.Payload, "conversationId");
                        var excludeUserId = GetGuid(message.Payload, "excludeUserId");
                        var msg = DeserializePayload<MessageResponseDto>(message.Payload["message"]);
                        await _realTimeNotifier.SendMessageToConversation(conversationId, excludeUserId, msg);
                    }
                    break;

                case nameof(IRealTimeNotifier.NotifyMessageEdited):
                    {
                        var conversationId = GetGuid(message.Payload, "conversationId");
                        var messageId = GetGuid(message.Payload, "messageId");
                        var newText = message.Payload["newText"].ToString()!;
                        var editedAt = DateTime.Parse(message.Payload["editedAt"].ToString()!);
                        await _realTimeNotifier.NotifyMessageEdited(conversationId, messageId, newText, editedAt);
                    }
                    break;

                case nameof(IRealTimeNotifier.NotifyMessageDeleted):
                    {
                        var conversationId = GetGuid(message.Payload, "conversationId");
                        var messageId = GetGuid(message.Payload, "messageId");
                        await _realTimeNotifier.NotifyMessageDeleted(conversationId, messageId);
                    }
                    break;

                default:
                    throw new Exception($"Unknown notification method: {message.MethodName}");
            }
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
