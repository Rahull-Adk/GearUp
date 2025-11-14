using GearUp.Domain.Entities.Cars;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface ICarRepository
    {
        Task<bool> IsUniqueVin(string vin);
        Task AddCarAsync(Car car);
        Task AddCarImagesAsync(IEnumerable<CarImage> carImages);
        Task<List<Car>> GetAllCarsAsync();
        Task<Car?> GetCarByIdAsync(Guid carId);
        Task<CarImage?> GetCarImageByCarIdAsync(Guid carId);
        void RemoveCarImageByCarId(Car car);
    }
}
