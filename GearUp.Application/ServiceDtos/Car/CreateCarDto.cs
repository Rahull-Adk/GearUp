using GearUp.Domain.Entities.Cars;
using GearUp.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace GearUp.Application.ServiceDtos.Car
{
    public class CreateCarRequestDto
    {
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Model { get; init; } = string.Empty;
        public string Make { get; init; } = string.Empty;
        public int Year { get; init; }
        public double Price { get; init; }
        public string Color { get; init; } = string.Empty;
        public int Mileage { get; init; }
        public int SeatingCapacity { get; init; }
        public int EngineCapacity { get; init; }
        public ICollection<IFormFile>? CarImages { get; init; }
        public FuelType FuelType { get; init; }
        public CarCondition CarCondition { get; init; }
        public TransmissionType TransmissionType { get; init; }
        public CarStatus CarStatus { get; init; }
        public string VIN { get; init; } = string.Empty;
        public string LicensePlate { get; init; } = string.Empty;
    }

    public class CreateCarResponseDto
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Model { get; init; } = string.Empty;
        public string Make { get; init; } = string.Empty;
        public int Year { get; init; }
        public double Price { get; init; }
        public string Color { get; init; } = string.Empty;
        public int Mileage { get; init; }
        public int SeatingCapacity { get; init; }
        public int EngineCapacity { get; init; }
        public ICollection<CarImage>? CarImages { get; init; }
        public FuelType FuelType { get; init; }
        public CarCondition CarCondition { get; init; }
        public TransmissionType TransmissionType { get; init; }
        public CarStatus CarStatus { get; init; }
        public CarValidationStatus CarValidationStatus { get; init; }
        public string VIN { get; init; } = string.Empty;
        public string LicensePlate { get; init; } = string.Empty;
    }
}
