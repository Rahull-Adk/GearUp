using GearUp.Application.Common;

namespace GearUp.Application.Messaging.Contracts
{
    public class ImageUploadMessage : IMessage
    {
        public Guid MessageId { get; set; } = Guid.NewGuid();
        public string? CorrelationId { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public Guid CarImageId { get; set; }
        public Guid CarId { get; set; }
        public Guid DealerId { get; set; }
    }
}
