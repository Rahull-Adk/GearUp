using System.Diagnostics.Eventing.Reader;
using System.Runtime.CompilerServices;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Domain.Entities;
using GearUp.Domain.Entities.Users;
using GearUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GearUp.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly GearUpDbContext _db;

        public UserRepository(GearUpDbContext db)
        {
            _db = db;
        }
        public async Task AddUserAsync(User user)
        {
            await _db.Users.AddAsync(user);
        }

        public async Task AddKycAsync(KycSubmissions kyc)
        {
            await _db.KycSubmissions.AddAsync(kyc);
        }

        public async Task<User?> GetUserByUsernameOrEmailAsync(string usernameOrEmail)
        {
            return await _db.Users.SingleOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);

        }

        public async Task<bool> UserExistAsync(Guid userId)
        {
            return await _db.Users.AnyAsync(u => u.Id == userId);
        }
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _db.Users.SingleOrDefaultAsync(u => u.Email == email);
        }

        public async Task<RegisterResponseDto?> GetUserByIdAsync(Guid id)
        {
            return await _db.Users.Where(u => u.Id == id)
                .Select(u => new RegisterResponseDto(
                    u.Id,
                    u.Provider,
                    u.Username,
                    u.Email,
                    u.Name,
                    u.Role,
                    u.DateOfBirth,
                    u.PhoneNumber,
                    u.AvatarUrl
                )).FirstOrDefaultAsync();
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _db.Users.SingleOrDefaultAsync(u => u.Username == username);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task<User?> GetUserEntityByIdAsync(Guid id)
        {
            return await _db.Users.SingleOrDefaultAsync(u => u.Id == id);
        }
    }
}
