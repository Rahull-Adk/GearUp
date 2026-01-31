using System.Threading.Tasks;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Domain.Entities.Cars;
using GearUp.Domain.Enums;
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

        public async Task<PageResult<CarResponseDto>> GetAllCarsAsync(int pageNum)
        {
            var query = _db.Cars
                .AsNoTracking()
                .Where(car => car.IsDeleted == false && car.ValidationStatus == CarValidationStatus.Approved && car.Status == CarStatus.Available)
                .Include(c => c.Images)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CarResponseDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Make = c.Make,
                    Model = c.Model,
                    DealerId = c.DealerId,
                    Year = c.Year,
                    Color = c.Color,
                    Price = c.Price,
                    VIN = c.VIN,
                    CarStatus = c.Status,
                    CarValidationStatus = c.ValidationStatus,
                    CreatedAt = c.CreatedAt,
                    CarImages = c.Images.Select(img => new CarImageDto
                    {
                        Id = img.Id,
                        CarId = img.CarId,
                        Url = img.Url
                    }).ToList()
                });

            var totalCars = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCars / 10.0);
            var cars = await query
                .Skip((pageNum - 1) * 10)
                .Take(10)
                .ToListAsync();

            return new PageResult<CarResponseDto>
            {
                TotalCount = totalCars,
                PageSize = 10,
                CurrentPage = pageNum,
                TotalPages = totalPages,
                Items = cars
            };
        }

        public async Task<PageResult<CarResponseDto>> GetMyCarsAsync(Guid dealerId, CarValidationStatus status, int pageNum)
        {
            var query = _db.Cars
                .AsNoTracking()
                .Where(car => car.IsDeleted == false && car.ValidationStatus == status && car.DealerId == dealerId)
                .Include(c => c.Images)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CarResponseDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Make = c.Make,
                    Model = c.Model,
                    DealerId = c.DealerId,
                    Year = c.Year,
                    Color = c.Color,
                    Price = c.Price,
                    VIN = c.VIN,
                    CarStatus = c.Status,
                    CarValidationStatus = c.ValidationStatus,
                    CreatedAt = c.CreatedAt,
                    CarImages = c.Images.Select(img => new CarImageDto
                    {
                        Id = img.Id,
                        CarId = img.CarId,
                        Url = img.Url
                    }).ToList()
                });

            var totalCars = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCars / 10.0);
            var cars = await query
                .Skip((pageNum - 1) * 10)
                .Take(10)
                .ToListAsync();

            return new PageResult<CarResponseDto>
            {
                TotalCount = totalCars,
                PageSize = 10,
                CurrentPage = pageNum,
                TotalPages = totalPages,
                Items = cars
            };
        }

        public async Task<PageResult<CarResponseDto>> GetDealerCarsAsync(Guid dealerId, int pageNum)
        {
            var query = _db.Cars
                .AsNoTracking()
                .Where(car => car.IsDeleted == false && car.ValidationStatus == CarValidationStatus.Approved && car.DealerId == dealerId)
                .Include(c => c.Images)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CarResponseDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Make = c.Make,
                    Model = c.Model,
                    DealerId = c.DealerId,
                    Year = c.Year,
                    Color = c.Color,
                    Price = c.Price,
                    VIN = c.VIN,
                    CarStatus = c.Status,
                    CarValidationStatus = c.ValidationStatus,
                    CreatedAt = c.CreatedAt,
                    CarImages = c.Images.Select(img => new CarImageDto
                    {
                        Id = img.Id,
                        CarId = img.CarId,
                        Url = img.Url
                    }).ToList()
                });

            var totalCars = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCars / 10.0);
            var cars = await query
                .Skip((pageNum - 1) * 10)
                .Take(10)
                .ToListAsync();

            return new PageResult<CarResponseDto>
            {
                TotalCount = totalCars,
                PageSize = 10,
                CurrentPage = pageNum,
                TotalPages = totalPages,
                Items = cars
            };
        }

        public async Task<PageResult<CarResponseDto>> SearchCarsAsync(CarSearchDto dto)
        {
            IQueryable<Car> query = _db.Cars.AsQueryable();
            query = query.Where(car => car.ValidationStatus == CarValidationStatus.Approved && car.Status == CarStatus.Available);
            if (!string.IsNullOrEmpty(dto.Query))
            {
                query = query.Where(c => c.Title.Contains(dto.Query) || c.Description.Contains(dto.Query) || c.Model.Contains(dto.Query) || c.Make.Contains(dto.Query));
            }

            if (!string.IsNullOrEmpty(dto.Color))
            {
                query = query.Where(c => c.Color == dto.Color);
            }

            if (dto.MinPrice.HasValue)
            {
                query = query.Where(c => c.Price >= dto.MinPrice.Value);
            }

            if (dto.MaxPrice.HasValue)
            {
                query = query.Where(c => c.Price <= dto.MaxPrice.Value);
            }

            if (!string.IsNullOrEmpty(dto.SortBy))
            {
                bool ascending = dto.SortOrder?.ToLower() == "asc";
                query = dto.SortBy.ToLower() switch
                {
                    "price" => ascending ? query.OrderBy(c => c.Price) : query.OrderByDescending(c => c.Price),
                    "year" => ascending ? query.OrderBy(c => c.Year) : query.OrderByDescending(c => c.Year),
                    _ => query
                };
            }
            else
            {
                query = query.OrderByDescending(c => c.CreatedAt);
            }

            var totalCars = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCars / 10.0);
            var cars = await query
                .AsNoTracking()
                .Include(c => c.Images)
                .Skip((dto.Page - 1) * 10)
                .Take(10)
                .Select(c => new CarResponseDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Make = c.Make,
                    Model = c.Model,
                    DealerId = c.DealerId,
                    Year = c.Year,
                    Color = c.Color,
                    Price = c.Price,
                    VIN = c.VIN,
                    CarStatus = c.Status,
                    CarValidationStatus = c.ValidationStatus,
                    CreatedAt = c.CreatedAt,
                    CarImages = c.Images.Select(img => new CarImageDto
                    {
                        Id = img.Id,
                        CarId = img.CarId,
                        Url = img.Url
                    }).ToList()
                })
                .ToListAsync();
            return new PageResult<CarResponseDto>
            {
                TotalCount = totalCars,
                PageSize = 10,
                CurrentPage = dto.Page,
                TotalPages = totalPages,
                Items = cars
            };
        }

        public async Task<Car?> GetCarEntityByIdAsync(Guid carId)
        {
            return await _db.Cars.Where(c => c.ValidationStatus == CarValidationStatus.Approved && c.Status == CarStatus.Available).Include(c => c.Images).FirstOrDefaultAsync(c => c.Id == carId);
        }

        public async Task<List<CarImageDto>> GetCarImagesByCarIdAsync(Guid carId)
        {
            return await _db.CarImages.AsNoTracking().Select(ci => new CarImageDto
            {
                Id = ci.Id,
                CarId = ci.CarId,
                Url = ci.Url
            }).Where(img => img.CarId == carId).ToListAsync();
        }

        public void RemoveCarImageByCarId(Car car)
        {
            _db.CarImages.RemoveRange(car.Images);
        }

        public async Task<CarResponseDto?> GetCarByIdAsync(Guid carId)
        {
            return await _db.Cars.Where(c => c.Id == carId)
                .AsNoTracking()
                .Include(c => c.Images)
                .Select(c => new CarResponseDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Make = c.Make,
                    Model = c.Model,
                    Year = c.Year,
                    Color = c.Color,
                    DealerId = c.DealerId,
                    Price = c.Price,
                    VIN = c.VIN,
                    CarStatus = c.Status,
                    CarValidationStatus = c.ValidationStatus,
                    CreatedAt = c.CreatedAt,
                    CarImages = c.Images.Select(img => new CarImageDto
                    {
                        Id = img.Id,
                        CarId = img.CarId,
                        Url = img.Url
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }
    }
}
