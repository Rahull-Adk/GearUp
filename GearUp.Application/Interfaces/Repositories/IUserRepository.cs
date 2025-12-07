using GearUp.Domain.Entities;
using GearUp.Domain.Entities.Users;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task AddUserAsync(User user);
        Task AddKycAsync(KycSubmissions kyc);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByIdAsync(Guid id);
        Task SaveChangesAsync();
        Task<Dictionary<Guid, User>> GetAllUserWithIds(List<Guid> userIds);
    }
}
