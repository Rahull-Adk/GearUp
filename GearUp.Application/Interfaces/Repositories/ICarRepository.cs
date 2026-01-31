using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Domain.Entities.Cars;
using GearUp.Domain.Enums;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface ICarRepository
    {
        Task<bool> IsUniqueVin(string vin);
        Task AddCarAsync(Car car);
        Task AddCarImagesAsync(IEnumerable<CarImage> carImages);
        Task<PageResult<CarResponseDto>> GetAllCarsAsync(int pageNum);
        Task<PageResult<CarResponseDto>> SearchCarsAsync(CarSearchDto dto);
        Task<PageResult<CarResponseDto>> GetMyCarsAsync(Guid dealerId, CarValidationStatus status, int pageNum);
        Task<CarResponseDto?> GetCarByIdAsync(Guid carId);
        Task<Car?> GetCarEntityByIdAsync(Guid carId);
        Task<List<CarImageDto>> GetCarImagesByCarIdAsync(Guid carId);
        Task<PageResult<CarResponseDto>> GetDealerCarsAsync(Guid dealerId, int pageNum);
        void RemoveCarImageByCarId(Car car);
    }
}
