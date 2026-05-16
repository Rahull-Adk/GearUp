using GearUp.Application.Common;

namespace GearUp.Application.Messaging.Contracts
{
    public class EmailRequestMessage : IMessage
    {
        public Guid MessageId { get; set; } = Guid.NewGuid();
        public string? CorrelationId { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public string ToEmail { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;

        public Dictionary<string, string> Payload { get; set; } = new();
    }
}
