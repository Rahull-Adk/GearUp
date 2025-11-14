using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.Car;

namespace GearUp.Application.Interfaces.Services.CarServiceInterface
{
    public interface ICarService
    {
        Task<Result<CreateCarResponseDto>> CreateCarAsync(CreateCarRequestDto request, Guid dealerId);
        Task<Result<ICollection<CreateCarResponseDto>>> GetAllCarsAsync();
        Task<Result<CreateCarResponseDto>> GetCarByIdAsync(Guid carId);
        Task<Result<CreateCarResponseDto>> UpdateCarAsync(Guid carId, UpdateCarDto request, Guid dealerId);
    }
}
