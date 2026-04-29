using GearUp.Domain.Enums;

namespace GearUp.Application.ServiceDtos.Car
{
    public class CarListDto
    {
        public Guid Id { get; init; }
        public string ThumbnailUrl { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Make { get; init; } = string.Empty;
        public string Model { get; init; } = string.Empty;
        public TransmissionType TransmissionType { get; init; }
        public CarValidationStatus CarValidationStatus { get; init; }
        public int Mileage { get; init; }
        public int SeatingCapacity { get; init; }
        public double Price { get; init; }
        public string Color { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }
}
