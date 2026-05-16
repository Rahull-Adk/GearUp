using System.Text.Json;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Messaging;
using RabbitMQ.Client;

namespace GearUp.Infrastructure.Messaging
{
    public sealed class RabbitMqPublisher : IMessagePublisher
    {
        private readonly IChannel _channel;
        private readonly RabbitMqOptions _options;

        public RabbitMqPublisher(IChannel channel, RabbitMqOptions options)
        {
            _channel = channel;
            _options = options;
        }

        public async Task PublishAsync<TMessage>(TMessage message, string routingKey, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            ArgumentNullException.ThrowIfNull(message);

            if (string.IsNullOrWhiteSpace(routingKey))
            {
                throw new ArgumentException("Routing key is required.", nameof(routingKey));
            }

            var body = JsonSerializer.SerializeToUtf8Bytes(message);
            var props = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = message.MessageId.ToString(),
                CorrelationId = message.CorrelationId ?? Guid.NewGuid().ToString("N")
            };

            await _channel.BasicPublishAsync(
                exchange: _options.Exchange,
                routingKey: routingKey,
                basicProperties: props,
                mandatory: false,
                body: body,
                cancellationToken: cancellationToken);
        }
    }
}