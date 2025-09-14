using GearUp.Domain.Entities.Users;
using GearUp.Domain.Enums;

namespace GearUp.Domain.Entities.Cars
{
    public class Car
    {
        public Guid Id { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string Model { get; private set; }
        public string Make { get; private set; }
        public int Year { get; private set; }
        public double Price { get; private set; }
        public string Color { get; private set; }
        public int Mileage { get; private set; }
        public int SeatingCapacity { get; private set; }
        public int EngineCapacity { get; private set; }
        public ICollection<CarImage>? ImageUrls { get; private set; }
        public FuelType FuelType { get; private set; }
        public CarCondition Condition { get; private set; }
        public TransmissionType Transmission { get; private set; }
        public CarStatus Status { get; private set; }
        public CarValidationStatus ValidationStatus { get; private set; }
        public CarPurpose Purpose { get; private set; }
        public double? RentalPricePerDay { get; private set; }
        public double? RentalPricePerWeek { get; private set; }
        public string VIN { get; private set; }
        public string LicensePlate { get; private set; }
        public Guid DealerId { get; private set; }
        public User Dealer { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private readonly ICollection<CarImage> _images = new List<CarImage>();
        public IReadOnlyCollection<CarImage> Images => _images.ToList().AsReadOnly();

        private Car()
        {
            
        }
        public static Car CreateForSale(
    string title,
    string description,
    string model,
    string make,
    int year,
    double price,
    string color,
    int mileage,
    int seatingCapacity,
    int engineCapacity,
    ICollection<CarImage>? imageUrls,
    FuelType fuelType,
    CarCondition condition,
    TransmissionType transmission,
    CarPurpose carPurpose,
    Guid dealerId,
    string vin,
    string licensePlate)
        {
            return new Car
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                Model = model,
                Make = make,
                Year = year,
                Price = price,
                Color = color,
                Mileage = mileage,
                SeatingCapacity = seatingCapacity,
                EngineCapacity = engineCapacity,
                ImageUrls = imageUrls,
                FuelType = fuelType,
                Condition = condition,
                Transmission = transmission,
                Status = CarStatus.Available,
                ValidationStatus = CarValidationStatus.Pending,
                Purpose = carPurpose,
                RentalPricePerDay = null,
                RentalPricePerWeek = null,
                VIN = vin,
                LicensePlate = licensePlate,
                DealerId = dealerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
        }


        public void UpdateDetails(string title, string description, double price, string color, int mileage)
        {
            Title = title;
            Description = description;
            Price = price;
            Color = color;
            Mileage = mileage;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsSold()
        {
            if (Status == CarStatus.Sold)
                throw new InvalidOperationException("Car is already sold.");

            Status = CarStatus.Sold;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            if (Status == CarStatus.Deleted)
                throw new InvalidOperationException("Car is already deleted.");

            Status = CarStatus.Deleted;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Reactivate()
        {
            if (Status != CarStatus.Deleted)
                throw new InvalidOperationException("Only deleted cars can be reactivated.");

            Status = CarStatus.Available;
            UpdatedAt = DateTime.UtcNow;
        }

    }
}
