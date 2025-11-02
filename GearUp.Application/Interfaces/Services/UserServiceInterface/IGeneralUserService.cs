using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.Auth;

namespace GearUp.Application.Interfaces.Services.UserServiceInterface
{
    public interface IGeneralUserService
    {
        Task<Result<RegisterResponseDto>> GetCurrentUserProfileService(string userId);
        Task<Result<RegisterResponseDto>> GetUserProfile(string username);
    }
}
