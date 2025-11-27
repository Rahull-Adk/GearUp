using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Domain.Entities.Cars;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface ICarRepository
    {
        Task<bool> IsUniqueVin(string vin);
        Task AddCarAsync(Car car);
        Task AddCarImagesAsync(IEnumerable<CarImage> carImages);
        Task<PageResult<Car>> GetAllCarsAsync(int pageNum);
        Task<PageResult<Car>> SearchCarsAsync(CarSearchDto dto);
        Task<Car?> GetCarByIdAsync(Guid carId);
        Task<CarImage?> GetCarImageByCarIdAsync(Guid carId);
        void RemoveCarImageByCarId(Car car);
    }
}
