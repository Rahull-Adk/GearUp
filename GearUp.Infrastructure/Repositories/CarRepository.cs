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

        public async Task<CursorPageResult<CarListDto>> GetAllCarsAsync(Cursor? cursor, CancellationToken cancellationToken = default)
        {
            IQueryable<Car> query = _db.Cars
                .AsNoTracking()
                .Where(car => car.IsDeleted == false && car.ValidationStatus == CarValidationStatus.Approved && car.Status == CarStatus.Available)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id);

            if (cursor is not null)
            {
                query = query.Where(c => c.CreatedAt < cursor.CreatedAt ||
                    (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) < 0));
            }

            var cars = await query
                .Take(PageSize + 1)
                .Select(c => new CarListDto
                {
                    Id = c.Id,
                    ThumbnailUrl = c.Images.OrderBy(img => img.Id).Select(img => img.Url).FirstOrDefault() ?? string.Empty,
                    Title = c.Title,
                    Make = c.Make,
                    Model = c.Model,
                    TransmissionType = c.Transmission,
                    Mileage = c.Mileage,
                    SeatingCapacity = c.SeatingCapacity,
                    Price = c.Price,
                    Color = c.Color,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync(cancellationToken);

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

            return new CursorPageResult<CarListDto>
            {
                Items = cars.Take(PageSize).ToList(),
                NextCursor = nextCursor,
                HasMore = hasMore
            };
        }

        public async Task<CursorPageResult<CarListDto>> GetMyCarsAsync(Guid dealerId, CarValidationStatus status, Cursor? cursor, CancellationToken cancellationToken = default)
        {
            IQueryable<Car> query = _db.Cars
                .AsNoTracking()
                .Where(car => car.IsDeleted == false && car.ValidationStatus == status && car.DealerId == dealerId)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id);

            if (cursor is not null)
            {
                query = query.Where(c => c.CreatedAt < cursor.CreatedAt ||
                    (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) < 0));
            }

            var cars = await query
                .Take(PageSize + 1)
                .Select(c => new CarListDto
                {
                    Id = c.Id,
                    ThumbnailUrl = c.Images.OrderBy(img => img.Id).Select(img => img.Url).FirstOrDefault() ?? string.Empty,
                    Title = c.Title,
                    Make = c.Make,
                    Model = c.Model,
                    TransmissionType = c.Transmission,
                    Mileage = c.Mileage,
                    SeatingCapacity = c.SeatingCapacity,
                    Price = c.Price,
                    Color = c.Color,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync(cancellationToken);

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

            return new CursorPageResult<CarListDto>
            {
                Items = cars.Take(PageSize).ToList(),
                NextCursor = nextCursor,
                HasMore = hasMore
            };
        }

        public async Task<CursorPageResult<CarListDto>> GetDealerCarsAsync(Guid dealerId, Cursor? cursor, CancellationToken cancellationToken = default)
        {
            IQueryable<Car> query = _db.Cars
                .AsNoTracking()
                .Where(car => car.IsDeleted == false && car.ValidationStatus == CarValidationStatus.Approved && car.DealerId == dealerId)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id);

            if (cursor is not null)
            {
                query = query.Where(c => c.CreatedAt < cursor.CreatedAt ||
                    (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) < 0));
            }

            var cars = await query
                .Take(PageSize + 1)
                .Select(c => new CarListDto
                {
                    Id = c.Id,
                    ThumbnailUrl = c.Images.OrderBy(img => img.Id).Select(img => img.Url).FirstOrDefault() ?? string.Empty,
                    Title = c.Title,
                    Make = c.Make,
                    Model = c.Model,
                    TransmissionType = c.Transmission,
                    Mileage = c.Mileage,
                    SeatingCapacity = c.SeatingCapacity,
                    Price = c.Price,
                    Color = c.Color,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync(cancellationToken);

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

            return new CursorPageResult<CarListDto>
            {
                Items = cars.Take(PageSize).ToList(),
                NextCursor = nextCursor,
                HasMore = hasMore
            };
        }

        public async Task<CursorPageResult<CarListDto>> SearchCarsAsync(CarSearchDto dto, Cursor? cursor, CancellationToken cancellationToken = default)
        {
            IQueryable<Car> query = _db.Cars.AsNoTracking();
            query = query.Where(car => car.ValidationStatus == CarValidationStatus.Approved && car.Status == CarStatus.Available);
            bool isAscending = string.Equals(dto.SortOrder, "asc", StringComparison.OrdinalIgnoreCase);
            bool isPriceSort = string.Equals(dto.SortBy, "price", StringComparison.OrdinalIgnoreCase);

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

            if (isPriceSort)
            {
                query = isAscending
                    ? query.OrderBy(c => c.Price).ThenBy(c => c.CreatedAt).ThenBy(c => c.Id)
                    : query.OrderByDescending(c => c.Price).ThenByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id);
            }
            else
            {
                query = isAscending
                    ? query.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
                    : query.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id);
            }

            if (cursor is not null)
            {
                if (isPriceSort)
                {
                    var cursorPrice = cursor.Price!.Value;
                    query = isAscending
                        ? query.Where(c => c.Price > cursorPrice ||
                            (c.Price == cursorPrice && c.CreatedAt > cursor.CreatedAt) ||
                            (c.Price == cursorPrice && c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) > 0))
                        : query.Where(c => c.Price < cursorPrice ||
                            (c.Price == cursorPrice && c.CreatedAt < cursor.CreatedAt) ||
                            (c.Price == cursorPrice && c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) < 0));
                }
                else
                {
                    query = isAscending
                        ? query.Where(c => c.CreatedAt > cursor.CreatedAt ||
                            (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) > 0))
                        : query.Where(c => c.CreatedAt < cursor.CreatedAt ||
                            (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) < 0));
                }
            }

            var cars = await query
                .Take(PageSize + 1)
                .Select(c => new CarListDto
                {
                    Id = c.Id,
                    ThumbnailUrl = c.Images.OrderBy(img => img.Id).Select(img => img.Url).FirstOrDefault() ?? string.Empty,
                    Title = c.Title,
                    Make = c.Make,
                    Model = c.Model,
                    TransmissionType = c.Transmission,
                    Mileage = c.Mileage,
                    SeatingCapacity = c.SeatingCapacity,
                    Price = c.Price,
                    Color = c.Color,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync(cancellationToken);

            bool hasMore = cars.Count > PageSize;
            string? nextCursor = null;

            if (hasMore)
            {
                var lastItem = cars[PageSize - 1];
                nextCursor = Cursor.Encode(new Cursor
                {
                    CreatedAt = lastItem.CreatedAt,
                    Price = isPriceSort ? lastItem.Price : null,
                    Id = lastItem.Id
                });
            }

            return new CursorPageResult<CarListDto>
            {
                Items = cars.Take(PageSize).ToList(),
                NextCursor = nextCursor,
                HasMore = hasMore
            };
        }

        public async Task<Car?> GetCarEntityByIdAsync(Guid carId)
        {
            return await _db.Cars.Where(c => c.ValidationStatus != CarValidationStatus.Default && c.Status == CarStatus.Available).Include(c => c.Images).FirstOrDefaultAsync(c => c.Id == carId);
        }

        public async Task<List<CarImageDto>> GetCarImagesByCarIdAsync(Guid carId, CancellationToken cancellationToken = default)
        {
            return await _db.CarImages.AsNoTracking().Where(img => img.CarId == carId).Select(ci => new CarImageDto
            {
                Id = ci.Id,
                CarId = ci.CarId,
                Url = ci.Url
            }).ToListAsync(cancellationToken);
        }

        public void RemoveCarImageByCarId(Car car)
        {
            _db.CarImages.RemoveRange(car.Images);
        }

        public async Task<CarResponseDto?> GetCarByIdAsync(Guid carId, CancellationToken cancellationToken = default)
        {
            return await _db.Cars.Where(c => c.Id == carId)
                .AsNoTracking()
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
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<CursorPageResult<CarListDto>> GetAllCarsForAdminAsync(Cursor? cursor, CancellationToken cancellationToken = default)
        {
            IQueryable<Car> query = _db.Cars
                .AsNoTracking()
                .Where(car => !car.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id);

            if (cursor is not null)
            {
                query = query.Where(c => c.CreatedAt < cursor.CreatedAt ||
                    (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) < 0));
            }

            var cars = await query
                .Take(PageSize + 1)
                .Select(c => new CarListDto
                {
                    Id = c.Id,
                    ThumbnailUrl = c.Images.OrderBy(img => img.Id).Select(img => img.Url).FirstOrDefault() ?? string.Empty,
                    Title = c.Title,
                    Make = c.Make,
                    Model = c.Model,
                    TransmissionType = c.Transmission,
                    Mileage = c.Mileage,
                    SeatingCapacity = c.SeatingCapacity,
                    Price = c.Price,
                    Color = c.Color,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync(cancellationToken);

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

            return new CursorPageResult<CarListDto>
            {
                Items = cars.Take(PageSize).ToList(),
                NextCursor = nextCursor,
                HasMore = hasMore
            };
        }

        public async Task<CursorPageResult<CarListDto>> GetCarsByValidationStatusAsync(CarValidationStatus status, Cursor? cursor, CancellationToken cancellationToken = default)
        {
            IQueryable<Car> query = _db.Cars
                .AsNoTracking()
                .Where(car => !car.IsDeleted && car.ValidationStatus == status)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id);

            if (cursor is not null)
            {
                query = query.Where(c => c.CreatedAt < cursor.CreatedAt ||
                    (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) < 0));
            }

            var cars = await query
                .Take(PageSize + 1)
                .Select(c => new CarListDto
                {
                    Id = c.Id,
                    ThumbnailUrl = c.Images.OrderBy(img => img.Id).Select(img => img.Url).FirstOrDefault() ?? string.Empty,
                    Title = c.Title,
                    Make = c.Make,
                    Model = c.Model,
                    TransmissionType = c.Transmission,
                    Mileage = c.Mileage,
                    SeatingCapacity = c.SeatingCapacity,
                    Price = c.Price,
                    Color = c.Color,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync(cancellationToken);

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

            return new CursorPageResult<CarListDto>
            {
                Items = cars.Take(PageSize).ToList(),
                NextCursor = nextCursor,
                HasMore = hasMore
            };
        }

        public async Task<CursorPageResult<CarListDto>> GetCarsByDealerIdForAdminAsync(Guid dealerId, Cursor? cursor, CancellationToken cancellationToken = default)
        {
            IQueryable<Car> query = _db.Cars
                .AsNoTracking()
                .Where(car => !car.IsDeleted && car.DealerId == dealerId)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id);

            if (cursor is not null)
            {
                query = query.Where(c => c.CreatedAt < cursor.CreatedAt ||
                    (c.CreatedAt == cursor.CreatedAt && c.Id.CompareTo(cursor.Id) < 0));
            }

            var cars = await query
                .Take(PageSize + 1)
                .Select(c => new CarListDto
                {
                    Id = c.Id,
                    ThumbnailUrl = c.Images.OrderBy(img => img.Id).Select(img => img.Url).FirstOrDefault() ?? string.Empty,
                    Title = c.Title,
                    Make = c.Make,
                    Model = c.Model,
                    TransmissionType = c.Transmission,
                    Mileage = c.Mileage,
                    SeatingCapacity = c.SeatingCapacity,
                    Price = c.Price,
                    Color = c.Color,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync(cancellationToken);

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

            return new CursorPageResult<CarListDto>
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
