using GearUp.Application.ServiceDtos.Auth;
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
        Task<RegisterResponseDto?> GetUserByIdAsync(Guid id);
        Task<User?> GetUserEntityByIdAsync(Guid id);
        Task SaveChangesAsync();
        Task<bool> UserExistAsync(Guid userId);
    }
}
