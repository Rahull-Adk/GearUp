using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Domain.Enums;

namespace GearUp.Application.Interfaces.Services.CarServiceInterface
{
    public interface ICarService
    {
        Task<Result<CarResponseDto>> CreateCarAsync(CreateCarRequestDto request, Guid dealerId);
        Task<Result<CursorPageResult<CarListDto>>> GetAllCarsAsync(string? cursor, CancellationToken cancellationToken = default);
        Task<Result<CarResponseDto>> GetCarByIdAsync(Guid carId, CancellationToken cancellationToken = default);
        Task<Result<CarResponseDto>> UpdateCarAsync(Guid carId, UpdateCarDto request, Guid dealerId);
        Task<Result<string>> DeleteCarByIdAsync(Guid carId, Guid dealerId);
        Task<Result<CursorPageResult<CarListDto>>> GetDealerCarsAsync(Guid dealerId, string? cursor, CancellationToken cancellationToken = default);
        Task<Result<CursorPageResult<CarListDto>>> SearchCarsAsync(CarSearchDto searchDto, string? cursor, CancellationToken cancellationToken = default);
        Task<Result<CursorPageResult<CarListDto>>> GetMyCarsAsync(Guid dealerId, CarValidationStatus status, string? cursor, CancellationToken cancellationToken = default);
    }
}
