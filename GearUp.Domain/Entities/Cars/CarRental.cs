

using GearUp.Domain.Entities.Users;
using GearUp.Domain.Enums;

namespace GearUp.Domain.Entities.Cars
{
    public class CarRental
    {
        public Guid Id { get; private set; }
        public Guid CarId { get; private set; }
        public Car Car { get; private set; }
        public Guid RenterId { get; private set; }
        public User Renter { get; private set; }
        public Guid TenantId { get; private set; }
        public User Tenant { get; private set; }

        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public decimal TotalPrice { get; private set; }
        public CarRentalStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public CarRental(Guid carId, Guid renterId, Guid tenantId, DateTime startDate, DateTime endDate, decimal totalPrice, CarRentalStatus status)
        {
            Id = Guid.NewGuid();
            CarId = carId;
            RenterId = renterId;
            TenantId = tenantId;
            StartDate = startDate;
            EndDate = endDate;
            TotalPrice = totalPrice;
            Status = status;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public static CarRental CreateRental(Guid carId, Guid renterId, Guid tenantId, DateTime startDate, DateTime endDate, decimal totalPrice, CarRentalStatus status)
        {
            if (carId == Guid.Empty)
                throw new ArgumentException("Car ID cannot be empty.", nameof(carId));
            if (renterId == Guid.Empty)
                throw new ArgumentException("Renter ID cannot be empty.", nameof(renterId));
            if (tenantId == Guid.Empty)
                throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
            if (startDate >= endDate)
                throw new ArgumentException("Start date must be before end date.", nameof(startDate));
            if (totalPrice < 0)
                throw new ArgumentException("Total price cannot be negative.", nameof(totalPrice));
            if (status == CarRentalStatus.Default)
                throw new ArgumentException("Status cannot be default.", nameof(status));
            return new CarRental(carId, renterId, tenantId, startDate, endDate, totalPrice, status);
        }
    }
}
