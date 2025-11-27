using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.Car;

namespace GearUp.Application.Interfaces.Services.CarServiceInterface
{
    public interface ICarService
    {
        Task<Result<CreateCarResponseDto>> CreateCarAsync(CreateCarRequestDto request, Guid dealerId);
        Task<Result<PageResult<CreateCarResponseDto>>> GetAllCarsAsync(int pageNum);
        Task<Result<CreateCarResponseDto>> GetCarByIdAsync(Guid carId);
        Task<Result<CreateCarResponseDto>> UpdateCarAsync(Guid carId, UpdateCarDto request, Guid dealerId);
        Task<Result<string>> DeleteCarByIdAsync(Guid carId, Guid dealerId);
        Task<Result<PageResult<CreateCarResponseDto>>> SearchCarsAsync(CarSearchDto searchDto);
    }
}
