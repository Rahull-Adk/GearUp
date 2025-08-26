using GearUp.Domain.Entities.Cars;

namespace GearUp.Domain.Repository_Interfaces
{
    public interface ICarRepository
    {
        Task<Car?> GetCarByIdAsync(Guid carId);
        Task<IEnumerable<Car>> GetAllCarsAsync();
        Task<IEnumerable<Car>> GetCarsByDealerIdAsync(Guid dealerId);
        Task<IEnumerable<Car>> GetCarsForSaleAsync();
        Task<IEnumerable<Car>> GetCarsForRentAsync();
        Task AddCarAsync(Car car);
        Task UpdateCarAsync(Car car);
        Task DeleteCarAsync(Guid carId);
        
    }
}
