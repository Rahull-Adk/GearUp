using System.Threading.Tasks;
using GearUp.Application.Common;
using GearUp.Application.Common.Pagination;
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
        private const int PageSize = 10;

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

        public async Task<CursorPageResult<CarResponseDto>> GetAllCarsAsync(Cursor? cursor)
        {
            IQueryable<Car> query = _db.Cars
                .AsNoTracking()
                .Where(car => car.IsDeleted == false && car.ValidationStatus == CarValidationStatus.Approved && car.Status == CarStatus.Available)
                .Include(c => c.Images)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id);

            if (cursor is not null)
            {
                query = query.Where(c => c.CreatedAt < cursor.CreatedAt ||
                    (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) < 0));
            }

            var cars = await query
                .Take(PageSize + 1)
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
                    Mileage = c.Mileage,
                    SeatingCapacity = c.SeatingCapacity,
                    EngineCapacity = c.EngineCapacity,
                    FuelType = c.FuelType,
                    CarCondition = c.Condition,
                    TransmissionType = c.Transmission,
                    VIN = c.VIN,
                    LicensePlate = c.LicensePlate,
                    CarStatus = c.Status,
                    CarValidationStatus = c.ValidationStatus,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    CarImages = c.Images.Select(img => new CarImageDto
                    {
                        Id = img.Id,
                        CarId = img.CarId,
                        Url = img.Url
                    }).ToList()
                })
                .ToListAsync();

            bool hasMore = cars.Count > PageSize;
            string? nextCursor = null;

            if (hasMore)
            {
                var lastItem = cars[PageSize - 1];
                nextCursor = Cursor.Encode(new Cursor
                {
                    CreatedAt = lastItem.CreatedAt,
                    Id = lastItem.Id
                });
            }

            return new CursorPageResult<CarResponseDto>
            {
                Items = cars.Take(PageSize).ToList(),
                NextCursor = nextCursor,
                HasMore = hasMore
            };
        }

        public async Task<CursorPageResult<CarResponseDto>> GetMyCarsAsync(Guid dealerId, CarValidationStatus status, Cursor? cursor)
        {
            IQueryable<Car> query = _db.Cars
                .AsNoTracking()
                .Where(car => car.IsDeleted == false && car.ValidationStatus == status && car.DealerId == dealerId)
                .Include(c => c.Images)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id);

            if (cursor is not null)
            {
                query = query.Where(c => c.CreatedAt < cursor.CreatedAt ||
                    (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) < 0));
            }

            var cars = await query
                .Take(PageSize + 1)
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
                    Mileage = c.Mileage,
                    SeatingCapacity = c.SeatingCapacity,
                    EngineCapacity = c.EngineCapacity,
                    FuelType = c.FuelType,
                    CarCondition = c.Condition,
                    TransmissionType = c.Transmission,
                    VIN = c.VIN,
                    LicensePlate = c.LicensePlate,
                    CarStatus = c.Status,
                    CarValidationStatus = c.ValidationStatus,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    CarImages = c.Images.Select(img => new CarImageDto
                    {
                        Id = img.Id,
                        CarId = img.CarId,
                        Url = img.Url
                    }).ToList()
                })
                .ToListAsync();

            bool hasMore = cars.Count > PageSize;
            string? nextCursor = null;

            if (hasMore)
            {
                var lastItem = cars[PageSize - 1];
                nextCursor = Cursor.Encode(new Cursor
                {
                    CreatedAt = lastItem.CreatedAt,
                    Id = lastItem.Id
                });
            }

            return new CursorPageResult<CarResponseDto>
            {
                Items = cars.Take(PageSize).ToList(),
                NextCursor = nextCursor,
                HasMore = hasMore
            };
        }

        public async Task<CursorPageResult<CarResponseDto>> GetDealerCarsAsync(Guid dealerId, Cursor? cursor)
        {
            IQueryable<Car> query = _db.Cars
                .AsNoTracking()
                .Where(car => car.IsDeleted == false && car.ValidationStatus == CarValidationStatus.Approved && car.DealerId == dealerId)
                .Include(c => c.Images)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id);

            if (cursor is not null)
            {
                query = query.Where(c => c.CreatedAt < cursor.CreatedAt ||
                    (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) < 0));
            }

            var cars = await query
                .Take(PageSize + 1)
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
                    Mileage = c.Mileage,
                    SeatingCapacity = c.SeatingCapacity,
                    EngineCapacity = c.EngineCapacity,
                    FuelType = c.FuelType,
                    CarCondition = c.Condition,
                    TransmissionType = c.Transmission,
                    VIN = c.VIN,
                    LicensePlate = c.LicensePlate,
                    CarStatus = c.Status,
                    CarValidationStatus = c.ValidationStatus,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    CarImages = c.Images.Select(img => new CarImageDto
                    {
                        Id = img.Id,
                        CarId = img.CarId,
                        Url = img.Url
                    }).ToList()
                })
                .ToListAsync();

            bool hasMore = cars.Count > PageSize;
            string? nextCursor = null;

            if (hasMore)
            {
                var lastItem = cars[PageSize - 1];
                nextCursor = Cursor.Encode(new Cursor
                {
                    CreatedAt = lastItem.CreatedAt,
                    Id = lastItem.Id
                });
            }

            return new CursorPageResult<CarResponseDto>
            {
                Items = cars.Take(PageSize).ToList(),
                NextCursor = nextCursor,
                HasMore = hasMore
            };
        }

        public async Task<CursorPageResult<CarResponseDto>> SearchCarsAsync(CarSearchDto dto, Cursor? cursor)
        {
            IQueryable<Car> query = _db.Cars.AsNoTracking();
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

            // Default ordering for cursor pagination
            query = query.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id);

            if (cursor is not null)
            {
                query = query.Where(c => c.CreatedAt < cursor.CreatedAt ||
                    (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) < 0));
            }

            var cars = await query
                .Include(c => c.Images)
                .Take(PageSize + 1)
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
                    Mileage = c.Mileage,
                    SeatingCapacity = c.SeatingCapacity,
                    EngineCapacity = c.EngineCapacity,
                    FuelType = c.FuelType,
                    CarCondition = c.Condition,
                    TransmissionType = c.Transmission,
                    VIN = c.VIN,
                    LicensePlate = c.LicensePlate,
                    CarStatus = c.Status,
                    CarValidationStatus = c.ValidationStatus,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    CarImages = c.Images.Select(img => new CarImageDto
                    {
                        Id = img.Id,
                        CarId = img.CarId,
                        Url = img.Url
                    }).ToList()
                })
                .ToListAsync();

            bool hasMore = cars.Count > PageSize;
            string? nextCursor = null;

            if (hasMore)
            {
                var lastItem = cars[PageSize - 1];
                nextCursor = Cursor.Encode(new Cursor
                {
                    CreatedAt = lastItem.CreatedAt,
                    Id = lastItem.Id
                });
            }

            return new CursorPageResult<CarResponseDto>
            {
                Items = cars.Take(PageSize).ToList(),
                NextCursor = nextCursor,
                HasMore = hasMore
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
                    Mileage = c.Mileage,
                    SeatingCapacity = c.SeatingCapacity,
                    EngineCapacity = c.EngineCapacity,
                    FuelType = c.FuelType,
                    CarCondition = c.Condition,
                    TransmissionType = c.Transmission,
                    VIN = c.VIN,
                    LicensePlate = c.LicensePlate,
                    CarStatus = c.Status,
                    CarValidationStatus = c.ValidationStatus,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    CarImages = c.Images.Select(img => new CarImageDto
                    {
                        Id = img.Id,
                        CarId = img.CarId,
                        Url = img.Url
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        // Admin methods
        public async Task<CursorPageResult<CarResponseDto>> GetAllCarsForAdminAsync(Cursor? cursor)
        {
            IQueryable<Car> query = _db.Cars
                .AsNoTracking()
                .Where(car => !car.IsDeleted)
                .Include(c => c.Images)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id);

            if (cursor is not null)
            {
                query = query.Where(c => c.CreatedAt < cursor.CreatedAt ||
                    (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) < 0));
            }

            var cars = await query
                .Take(PageSize + 1)
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

            bool hasMore = cars.Count > PageSize;
            string? nextCursor = null;

            if (hasMore)
            {
                var lastItem = cars[PageSize - 1];
                nextCursor = Cursor.Encode(new Cursor
                {
                    CreatedAt = lastItem.CreatedAt,
                    Id = lastItem.Id
                });
            }

            return new CursorPageResult<CarResponseDto>
            {
                Items = cars.Take(PageSize).ToList(),
                NextCursor = nextCursor,
                HasMore = hasMore
            };
        }

        public async Task<CursorPageResult<CarResponseDto>> GetCarsByValidationStatusAsync(CarValidationStatus status, Cursor? cursor)
        {
            IQueryable<Car> query = _db.Cars
                .AsNoTracking()
                .Where(car => !car.IsDeleted && car.ValidationStatus == status)
                .Include(c => c.Images)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id);

            if (cursor is not null)
            {
                query = query.Where(c => c.CreatedAt < cursor.CreatedAt ||
                    (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) < 0));
            }

            var cars = await query
                .Take(PageSize + 1)
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

            bool hasMore = cars.Count > PageSize;
            string? nextCursor = null;

            if (hasMore)
            {
                var lastItem = cars[PageSize - 1];
                nextCursor = Cursor.Encode(new Cursor
                {
                    CreatedAt = lastItem.CreatedAt,
                    Id = lastItem.Id
                });
            }

            return new CursorPageResult<CarResponseDto>
            {
                Items = cars.Take(PageSize).ToList(),
                NextCursor = nextCursor,
                HasMore = hasMore
            };
        }

        public async Task<CursorPageResult<CarResponseDto>> GetCarsByDealerIdForAdminAsync(Guid dealerId, Cursor? cursor)
        {
            IQueryable<Car> query = _db.Cars
                .AsNoTracking()
                .Where(car => !car.IsDeleted && car.DealerId == dealerId)
                .Include(c => c.Images)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id);

            if (cursor is not null)
            {
                query = query.Where(c => c.CreatedAt < cursor.CreatedAt ||
                    (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) < 0));
            }

            var cars = await query
                .Take(PageSize + 1)
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

            bool hasMore = cars.Count > PageSize;
            string? nextCursor = null;

            if (hasMore)
            {
                var lastItem = cars[PageSize - 1];
                nextCursor = Cursor.Encode(new Cursor
                {
                    CreatedAt = lastItem.CreatedAt,
                    Id = lastItem.Id
                });
            }

            return new CursorPageResult<CarResponseDto>
            {
                Items = cars.Take(PageSize).ToList(),
                NextCursor = nextCursor,
                HasMore = hasMore
            };
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
