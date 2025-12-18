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
        Task<PageResult<CarResponseDto>> GetAllCarsAsync(int pageNum);
        Task<PageResult<CarResponseDto>> SearchCarsAsync(CarSearchDto dto);
        Task<CarResponseDto?> GetCarByIdAsync(Guid carId);
        Task<Car?> GetCarEntityByIdAsync(Guid carId);
        Task<List<CarImageDto>> GetCarImagesByCarIdAsync(Guid carId);
        void RemoveCarImageByCarId(Car car);
    }
}
