using GearUp.Application.Interfaces.Repositories;
using GearUp.Domain.Entities.Users;
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

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _db.Users.SingleOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await _db.Users.SingleOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _db.Users.SingleOrDefaultAsync(u => u.Username == username);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
