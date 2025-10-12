
namespace GearUp.Domain.Entities.Cars
{
    public class CarImage
    {
        public Guid Id { get; private set; }
        public string Url { get; private set; } = string.Empty;

        public Guid CarId { get; private set; }
        public Car Car { get; private set; } = null!;
    }
}
