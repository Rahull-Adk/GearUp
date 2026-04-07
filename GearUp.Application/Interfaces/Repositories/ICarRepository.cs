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
        Task<CursorPageResult<CarListDto>> GetAllCarsAsync(Cursor? cursor, CancellationToken cancellationToken = default);
        Task<CursorPageResult<CarListDto>> SearchCarsAsync(CarSearchDto dto, Cursor? cursor, CancellationToken cancellationToken = default);
        Task<CursorPageResult<CarListDto>> GetMyCarsAsync(Guid dealerId, CarValidationStatus status, Cursor? cursor, CancellationToken cancellationToken = default);
        Task<CarResponseDto?> GetCarByIdAsync(Guid carId, CancellationToken cancellationToken = default);
        Task<Car?> GetCarEntityByIdAsync(Guid carId);
        Task<List<CarImageDto>> GetCarImagesByCarIdAsync(Guid carId, CancellationToken cancellationToken = default);
        Task<CursorPageResult<CarListDto>> GetDealerCarsAsync(Guid dealerId, Cursor? cursor, CancellationToken cancellationToken = default);
        void RemoveCarImageByCarId(Car car);

        // Admin methods
        Task<CursorPageResult<CarListDto>> GetAllCarsForAdminAsync(Cursor? cursor, CancellationToken cancellationToken = default);
        Task<CursorPageResult<CarListDto>> GetCarsByValidationStatusAsync(CarValidationStatus status, Cursor? cursor, CancellationToken cancellationToken = default);
        Task<CursorPageResult<CarListDto>> GetCarsByDealerIdForAdminAsync(Guid dealerId, Cursor? cursor, CancellationToken cancellationToken = default);
        Task SaveChangesAsync();
    }
}
