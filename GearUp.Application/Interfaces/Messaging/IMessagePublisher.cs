using GearUp.Application.Common;

namespace GearUp.Application.Interfaces.Messaging
{
    public interface IMessagePublisher
    {
        Task PublishAsync<TMessage>(TMessage message, string routingKey, CancellationToken cancellationToken = default) where TMessage : IMessage;
    }
}