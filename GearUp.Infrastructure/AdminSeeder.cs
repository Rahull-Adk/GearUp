using GearUp.Domain.Entities.Users;
using GearUp.Domain.Enums;
using GearUp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GearUp.Infrastructure
{
    public static class AdminSeeder
    {
        public static async Task SeedAdminAsync(GearUpDbContext db, IPasswordHasher<User> hasher, string adminUsername, string adminEmail, string adminPassword)
        {
            var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == adminEmail && u.Role == UserRole.Admin);
            if (admin == null)
            {
                var newAdmin =  User.CreateLocalUser(adminUsername, adminEmail, "Admin");
                newAdmin.SetPassword(hasher.HashPassword(newAdmin, adminPassword));
                newAdmin.SetRole(UserRole.Admin);
                await db.Users.AddAsync(newAdmin);
                await db.SaveChangesAsync();
            }
        }
    }
}
