using System.Text.Json.Serialization;
using GearUp.Domain.Enums;

namespace GearUp.Domain.Entities.Cars
{
    public class CarImage
    {
        public Guid Id { get; private set; }
        public string Url { get; private set; } = string.Empty;
        public Guid CarId { get; private set; }
        [JsonIgnore]
        public Car? Car { get; private set; } = null!;

        public ImageProcessingStatus Status { get; private set; }
        public string? LocalFilePath { get; private set; }
        public string? ErrorMessage { get; private set; }

        public static CarImage CreateCarImage(Guid carId, string url, ImageProcessingStatus status = ImageProcessingStatus.Completed, string? localFilePath = null)
        {
            return new CarImage
            {
                Id = Guid.NewGuid(),
                CarId = carId,
                Url = url,
                Status = status,
                LocalFilePath = localFilePath
            };
        }

        public void SetStatus(ImageProcessingStatus status)
        {
            Status = status;
        }

        public void SetUrl(string url)
        {
            Url = url;
        }

        public void SetLocalFilePath(string? path)
        {
            LocalFilePath = path;
        }

        public void SetError(string? message)
        {
            ErrorMessage = message;
            Status = ImageProcessingStatus.Failed;
        }
    }
}