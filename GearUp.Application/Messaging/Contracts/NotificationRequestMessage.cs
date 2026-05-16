using GearUp.Application.Common;

namespace GearUp.Application.Messaging.Contracts
{
    public class NotificationRequestMessage : IMessage
    {
        public Guid MessageId { get; set; } = Guid.NewGuid();
        public string? CorrelationId { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public string MethodName { get; set; } = string.Empty;
        public Dictionary<string, object> Payload { get; set; } = new();
    }
}
