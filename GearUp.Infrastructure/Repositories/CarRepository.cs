using System.Threading.Tasks;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Domain.Entities.Cars;
using GearUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GearUp.Infrastructure.Repositories
{
    public class CarRepository : ICarRepository
    {
        private readonly GearUpDbContext _db;

        public CarRepository(GearUpDbContext db)
        {
            _db = db;
        }

        public async Task<bool> IsUniqueVin(string vin)
        {
            return await _db.Cars.AsNoTracking().AnyAsync(c => c.VIN == vin);
        }

        public async Task AddCarAsync(Car car)
        {
            await _db.Cars.AddAsync(car);
        }
        public async Task AddCarImagesAsync(IEnumerable<CarImage> carImages)
        {
            await _db.CarImages.AddRangeAsync(carImages);
        }
        public async Task<List<Car>> GetAllCarsAsync()
        {
            return await _db.Cars.AsNoTracking().Include(c => c.Images).ToListAsync();
        }

        public async Task<Car?> GetCarByIdAsync(Guid carId)
        {
            return await _db.Cars.AsNoTracking().Include(c => c.Images).FirstOrDefaultAsync(c => c.Id == carId);
        }

        public async Task<CarImage?> GetCarImageByCarIdAsync(Guid carId)
        {
            return await _db.CarImages.AsNoTracking().FirstOrDefaultAsync(img => img.CarId == carId);
        }

        public void RemoveCarImageByCarId(Car car)
        {
            _db.CarImages.RemoveRange(car.Images);
        }

    }
}
