using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GearUp.Infrastructure.Persistence
{
    public class GearUpDbContextFactory : IDesignTimeDbContextFactory<GearUpDbContext>
    {
        public GearUpDbContext CreateDbContext(string[] args)
        {
            Env.Load(Path.Combine(FindSolutionRoot(), ".env"));

            var optionsBuilder = new DbContextOptionsBuilder<GearUpDbContext>();
            var connectionString =
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Please setup your connection string.");
            }

            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return new GearUpDbContext(optionsBuilder.Options);
        }

        private static string FindSolutionRoot()
        {
            var dir = Directory.GetCurrentDirectory();

            while (!File.Exists(Path.Combine(dir, ".env")))
            {
                dir = Directory.GetParent(dir)?.FullName
                      ?? throw new Exception("Could not locate .env file");
            }

            return dir;
        }

    }
}