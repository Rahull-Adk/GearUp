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


        public async Task<bool> UserExistAsync(Guid userId)
        {
            return await _db.Users.AnyAsync(u => u.Id == userId);
        }
        public async Task<User?> GetUserEntityByEmailAsync(string email)
        {
            return await _db.Users.SingleOrDefaultAsync(u => u.Email == email);
        }
        public async Task<RegisterResponseDto?> GetUserByEmailAsync(string email)
        {
            return await _db.Users.Where(u => u.Email == email)
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

        public async Task<RegisterResponseDto?> GetUserByUsernameAsync(string username)
        {
            return await _db.Users.Where(u => u.Username == username)
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

        public async Task<User?> GetUserEntityByUsernameAsync(string username)
        {
            return await _db.Users.Where(u => u.Username == username).FirstOrDefaultAsync();
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
