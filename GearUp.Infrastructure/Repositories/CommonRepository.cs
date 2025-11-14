using GearUp.Application.Interfaces.Repositories;
using GearUp.Infrastructure.Persistence;

namespace GearUp.Infrastructure.Repositories
{
    public class CommonRepository: ICommonRepository
    {
        private readonly GearUpDbContext _db;
        public CommonRepository(GearUpDbContext db)
        {
            _db = db;
        }
        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
