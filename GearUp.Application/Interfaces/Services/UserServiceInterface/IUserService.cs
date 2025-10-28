using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.ServiceDtos.User;

namespace GearUp.Application.Interfaces.Services.UserServiceInterface
{
    public interface IUserService
    {
        Task<Result<RegisterResponseDto>> GetCurrentUserProfileService(string userId);
        Task<Result<UpdateUserResponseDto>> UpdateUserProfileService(string userId, UpdateUserRequestDto updateUserProfileDto);
        Task<Result<KycResponseDto>> KycService(string userId, KycRequestDto req);
        Task<Result<RegisterResponseDto>> GetUserProfile(string username);
    }
}
