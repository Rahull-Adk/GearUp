
namespace GearUp.Domain.Enums
{

    public enum FuelType
    {
        Default = 0,
        Petrol = 1,
        Diesel = 2,
        Electric = 3,
        Hybrid = 4
    }
    public enum CarStatus
    {
        Default = 0,
        Available = 1,
        Sold = 2,
        Reserved = 3,
        Unavailable = 4,
        Deleted = 5
    }
    public enum TransmissionType
    {
        Default = 0,
        Manual = 1,
        Automatic = 2,
        SemiAutomatic = 3
    }
    public enum CarCondition
    {
        Default = 0,
        New = 1,
        Used = 2
    }

    public enum CarValidationStatus
    {
        Default = 0,
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }

    public enum CarPurpose
    {
        Default = 0,
        ForSale = 1,
        ForRent = 2,
    }

    public enum CarRentalStatus
    {
        Default = 0,
        Pending = 1,
        Confirmed = 2,
        Completed = 3,
        Cancelled = 4
    }

    public enum AppointmentStatus
    {
        Default = 0,
        Scheduled = 1,
        Completed = 2,
        Cancelled = 3,
        NoShow = 4
    }

}
