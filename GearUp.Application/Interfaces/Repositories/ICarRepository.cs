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
        Task<Dictionary<Guid, Car>> GetCarsByIdsAsync(List<Guid> carIds);
        Task<Car?> GetCarByIdAsync(Guid carId);
        Task<List<CarImageDto>> GetCarImagesByCarIdAsync(Guid carId);
        void RemoveCarImageByCarId(Car car);
    }
}
