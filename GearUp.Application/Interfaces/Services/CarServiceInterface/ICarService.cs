using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Domain.Enums;

namespace GearUp.Application.Interfaces.Services.CarServiceInterface
{
    public interface ICarService
    {
        Task<Result<CarResponseDto>> CreateCarAsync(CreateCarRequestDto request, Guid dealerId);
        Task<Result<CursorPageResult<CarResponseDto>>> GetAllCarsAsync(string? cursor);
        Task<Result<CarResponseDto>> GetCarByIdAsync(Guid carId);
        Task<Result<CarResponseDto>> UpdateCarAsync(Guid carId, UpdateCarDto request, Guid dealerId);
        Task<Result<string>> DeleteCarByIdAsync(Guid carId, Guid dealerId);
        Task<Result<CursorPageResult<CarResponseDto>>> GetDealerCarsAsync(Guid dealerId, string? cursor);
        Task<Result<CursorPageResult<CarResponseDto>>> SearchCarsAsync(CarSearchDto searchDto, string? cursor);
        Task<Result<CursorPageResult<CarResponseDto>>> GetMyCarsAsync(Guid dealerId, CarValidationStatus status, string? cursor);
    }
}
