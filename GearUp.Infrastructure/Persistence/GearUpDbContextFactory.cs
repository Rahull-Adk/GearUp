using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace GearUp.Infrastructure.Persistence
{
    public class GearUpDbContextFactory : IDesignTimeDbContextFactory<GearUpDbContext>
    {
        public GearUpDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<GearUpDbContext>();
            var connectionString = Environment.GetEnvironmentVariable("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Please setup your connection string.");
            }
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return new GearUpDbContext(optionsBuilder.Options);
        }
    }
}
