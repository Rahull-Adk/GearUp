using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.User;

namespace GearUp.Application.Interfaces.Services.UserServiceInterface
{
    public interface IProfileUpdateService
    {
        Task<Result<UpdateUserResponseDto>> UpdateUserProfileService(string userId, UpdateUserRequestDto reqDto);
    }
}
