using GearUp.Domain.Entities.Users;
using GearUp.Domain.Enums;
using GearUp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GearUp.Infrastructure.Seed
{
    public static class AdminSeeder
    {
        public static async Task SeedAdminAsync(GearUpDbContext db, IPasswordHasher<User> hasher, string adminUsername, string adminEmail, string adminPassword)
        {
            if (string.IsNullOrWhiteSpace(adminUsername)) throw new InvalidOperationException("ADMIN_USERNAME not configured");
            if (string.IsNullOrWhiteSpace(adminEmail)) throw new InvalidOperationException("ADMIN_EMAIL not configured");
            if (string.IsNullOrWhiteSpace(adminPassword) || adminPassword == "string")
                throw new InvalidOperationException("ADMIN_PASSWORD not configured (placeholder 'string' detected). Set a real value in environment variables.");

            var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == adminEmail && u.Role == UserRole.Admin);
            if (admin == null)
            {
                var newAdmin = User.CreateLocalUser(adminUsername, adminEmail, "Admin");
                newAdmin.SetPassword(hasher.HashPassword(newAdmin, adminPassword));
                newAdmin.SetRole(UserRole.Admin);
                await db.Users.AddAsync(newAdmin);
                await db.SaveChangesAsync();
            }
        }
    }
}
