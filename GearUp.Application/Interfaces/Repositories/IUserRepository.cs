using GearUp.Domain.Entities.Users;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        public Task AddUserAsync(User user);
        public Task<User?> GetUserByEmailAsync(string email);
        public Task<User?> GetUserByUsernameAsync(string username);
        public Task<User?> GetUserByIdAsync(Guid id);
        public Task SaveChangesAsync();
    }
}
