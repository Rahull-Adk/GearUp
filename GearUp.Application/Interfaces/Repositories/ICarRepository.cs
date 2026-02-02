using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
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
        Task<CursorPageResult<CarResponseDto>> GetAllCarsAsync(Cursor? cursor);
        Task<CursorPageResult<CarResponseDto>> SearchCarsAsync(CarSearchDto dto, Cursor? cursor);
        Task<CursorPageResult<CarResponseDto>> GetMyCarsAsync(Guid dealerId, CarValidationStatus status, Cursor? cursor);
        Task<CarResponseDto?> GetCarByIdAsync(Guid carId);
        Task<Car?> GetCarEntityByIdAsync(Guid carId);
        Task<List<CarImageDto>> GetCarImagesByCarIdAsync(Guid carId);
        Task<CursorPageResult<CarResponseDto>> GetDealerCarsAsync(Guid dealerId, Cursor? cursor);
        void RemoveCarImageByCarId(Car car);
    }
}
