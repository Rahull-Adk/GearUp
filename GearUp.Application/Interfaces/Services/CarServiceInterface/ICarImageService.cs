using GearUp.Application.Common;
using GearUp.Domain.Entities.Cars;
using Microsoft.AspNetCore.Http;

namespace GearUp.Application.Interfaces.Services.CarServiceInterface
{
    public interface ICarImageService
    {
        Task<Result<List<CarImage>>> ProcessForCreateAsync(ICollection<IFormFile> files, Guid dealerId, Guid carId);
        Task<Result<List<CarImage>>> ProcessForUpdateAsync(Car existingCar, ICollection<IFormFile>? files, Guid dealerId);
    }
}
