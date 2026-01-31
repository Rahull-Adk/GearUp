using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Domain.Enums;

namespace GearUp.Application.Interfaces.Services.CarServiceInterface
{
    public interface ICarService
    {
        Task<Result<CarResponseDto>> CreateCarAsync(CreateCarRequestDto request, Guid dealerId);
        Task<Result<PageResult<CarResponseDto>>> GetAllCarsAsync(int pageNum);
        Task<Result<CarResponseDto>> GetCarByIdAsync(Guid carId);
        Task<Result<CarResponseDto>> UpdateCarAsync(Guid carId, UpdateCarDto request, Guid dealerId);
        Task<Result<string>> DeleteCarByIdAsync(Guid carId, Guid dealerId);
        Task<Result<PageResult<CarResponseDto>>> GetDealerCarsAsync(Guid dealerId, int pageNum);
        Task<Result<PageResult<CarResponseDto>>> SearchCarsAsync(CarSearchDto searchDto);
        Task<Result<PageResult<CarResponseDto>>> GetMyCarsAsync(Guid dealerId, CarValidationStatus status, int pageNum);
    }
}
