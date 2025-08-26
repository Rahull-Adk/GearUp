
using GearUp.Domain.Entities.Users;

namespace GearUp.Domain.Repository_Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserByEmailAsync(string email);
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task SaveChanges();
       
    }
}
