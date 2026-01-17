using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GearUp.Domain.Entities.Users
{
    public class UserPreference
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }

        public string CarMake { get; private set; } = string.Empty;
        public string CarModel { get; private set; } = string.Empty;
        public string CarColor { get; private set; } = string.Empty;
        public double Price { get; private set; }
        [JsonIgnore] public User User { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private UserPreference()
        {
            Id = Guid.NewGuid();
        }

        public static UserPreference Create(Guid userId, string carMake, string carModel, string carColor, double price)
        {
            return new UserPreference
            {
                UserId = userId,
                CarMake = carMake,
                CarColor = carColor,
                CarModel = carModel,
                Price = price,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}