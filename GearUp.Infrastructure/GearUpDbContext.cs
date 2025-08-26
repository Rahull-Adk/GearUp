using Microsoft.EntityFrameworkCore;

namespace GearUp.Infrastructure
{
    public class GearUpDbContext : DbContext
    {
        public GearUpDbContext(DbContextOptions<GearUpDbContext> options) : base(options)
        {
        }


    }
}
