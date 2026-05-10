namespace GearUp.Application.Common
{
    public interface IMessage
    {
        Guid MessageId { get; }
        string? CorrelationId { get; }
        DateTimeOffset CreatedAtUtc { get; }
    }
}