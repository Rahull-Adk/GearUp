using GearUp.Application.Common;
using GearUp.Application.Interfaces.Messaging;
using Microsoft.Extensions.Logging;

namespace GearUp.Infrastructure.Messaging
{
    public sealed class InMemoryPublisher : IMessagePublisher
    {
        private readonly ILogger<InMemoryPublisher> _logger;

        public InMemoryPublisher(ILogger<InMemoryPublisher> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync<TMessage>(TMessage message, string routingKey, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            _logger.LogInformation("[IN-MEMORY QUEUE] Publishing message of type {MessageType} with routing key {RoutingKey}", typeof(TMessage).Name, routingKey);
            // In a more advanced implementation, we could use an internal Channel or an In-Memory Bus to trigger consumers.
            // For now, this prevents the app from crashing when RabbitMQ is disabled.
            return Task.CompletedTask;
        }
    }
}
