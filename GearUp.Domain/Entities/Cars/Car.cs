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
        public FuelType FuelType { get; private set; }
        public CarCondition Condition { get; private set; }
        public TransmissionType Transmission { get; private set; }
        public CarStatus Status { get; private set; }
        public CarValidationStatus ValidationStatus { get; private set; }
        public double? RentalPricePerDay { get; private set; }
        public double? RentalPricePerWeek { get; private set; }
        public string VIN { get; private set; }
        public string LicensePlate { get; private set; }
        public Guid DealerId { get; private set; }
        public User Dealer { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private ICollection<CarImage> _images = new List<CarImage>();
        public IReadOnlyCollection<CarImage> Images => (IReadOnlyCollection<CarImage>)_images;

        private Car()
        {

        }
        public static Car CreateForSale(
            Guid Id,
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
                FuelType = fuelType,
                Condition = condition,
                Transmission = transmission,
                _images = imageUrls,
                Status = CarStatus.Available,
                ValidationStatus = CarValidationStatus.Pending,
                VIN = vin,
                LicensePlate = licensePlate,
                DealerId = dealerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
        }

        public void UpdateDetails(
        string title, string description, string model, string make, int year, double price,
        string color, int mileage, int seatingCapacity, int engineCapacity,
        ICollection<CarImage>? imageUrls,
        FuelType? fuelType, CarCondition? condition, TransmissionType? transmission)
        {
            if (Status is CarStatus.Sold or CarStatus.Deleted)
                throw new InvalidOperationException("Cannot update details of a sold or deleted car.");

            Title = string.IsNullOrEmpty(title) ? Title : title;
            Description = string.IsNullOrEmpty(description) ? Description : description;
            Model = string.IsNullOrEmpty(model) ? Model : model;
            Make = string.IsNullOrEmpty(make) ? Make : make;
            Color = string.IsNullOrEmpty(color) ? Color : color;
            Year = year > 0 ? year : Year;
            Price = price >= 0 ? price : Price;
            Mileage = mileage > 0 ? mileage : Mileage;
            SeatingCapacity = seatingCapacity > 0 ? seatingCapacity : SeatingCapacity;
            EngineCapacity = engineCapacity > 0 ? engineCapacity : EngineCapacity;
            FuelType = FuelType != FuelType.Default && fuelType.HasValue ? fuelType.Value : FuelType;
            Condition = Condition != CarCondition.Default && condition.HasValue ? condition.Value : Condition;
            Transmission = Transmission != TransmissionType.Default && transmission.HasValue ? transmission.Value : Transmission;

            if (imageUrls?.Count > 0)
                _images = imageUrls;

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
