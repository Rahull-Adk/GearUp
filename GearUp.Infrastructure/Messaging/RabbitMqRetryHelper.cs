using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GearUp.Infrastructure.Messaging;

public static class RabbitMqRetryHelper
{
    public static async Task HandleMessageFailureAsync(
        IChannel channel,
        BasicDeliverEventArgs ea,
        RabbitMqOptions options,
        ILogger logger,
        Exception e,
        CancellationToken stoppingToken)
    {
        var count = 0;
        if (ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.TryGetValue("x-retry-count", out var countObj))
        {
            if (countObj is int c) count = c;
        }

        if (count < options.MaxRetries)
        {
            logger.LogWarning(e, "Message failed processing. Retrying {Attempt} of {MaxRetries}. CorrelationId: {CorrelationId}", count + 1, options.MaxRetries, ea.BasicProperties.CorrelationId);
            
            var properties = new BasicProperties
            {
                MessageId = ea.BasicProperties.MessageId,
                CorrelationId = ea.BasicProperties.CorrelationId,
                Headers = ea.BasicProperties.Headers != null ? new Dictionary<string, object?>(ea.BasicProperties.Headers) : new Dictionary<string, object?>(),
                ContentType = ea.BasicProperties.ContentType,
                Persistent = true
            };
            properties.Headers["x-retry-count"] = count + 1;

            await channel.BasicPublishAsync(
                exchange: options.RetryExchange,
                routingKey: ea.RoutingKey, // Preserve original routing key for DLX return
                basicProperties: properties,
                body: ea.Body,
                mandatory: false,
                cancellationToken: stoppingToken
            );
            await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
        }
        else
        {
            logger.LogError(e, "Message failed after {MaxRetries} retries. Sending to DLQ. CorrelationId: {CorrelationId}", options.MaxRetries, ea.BasicProperties.CorrelationId);
            await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, cancellationToken: stoppingToken);
        }
    }
}
