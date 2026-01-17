using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Domain.Entities;
using GearUp.Domain.Entities.Users;

namespace GearUp.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task AddUserAsync(User user);
        Task AddKycAsync(KycSubmissions kyc);
        Task<User?> GetUserEntityByEmailAsync(string email);
        Task<RegisterResponseDto?> GetUserByUsernameAsync(string username);
        Task<RegisterResponseDto?> GetUserByIdAsync(Guid id);
        Task<RegisterResponseDto?> GetUserByEmailAsync(string email);

        Task<User?> GetUserEntityByUsernameAsync(string username);
        Task<User?> GetUserEntityByIdAsync(Guid id);
        Task SaveChangesAsync();
        Task<bool> UserExistAsync(Guid userId);
    }
}
