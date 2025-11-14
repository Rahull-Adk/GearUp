
using System.Text.Json.Serialization;

namespace GearUp.Domain.Entities.Cars
{
    public class CarImage
    {
        public Guid Id { get; private set; }
        public string Url { get; private set; } = string.Empty;
        public Guid CarId { get; private set; }
        [JsonIgnore]
        public Car Car { get; private set; } = null!;

        public static CarImage CreateCarImage(Guid carId, string url)
        {
            return new CarImage
            {
                Id = Guid.NewGuid(),
                CarId = carId,
                Url = url
            };
        }
    }
}