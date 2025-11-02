using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.User;

namespace GearUp.Application.Interfaces.Services.UserServiceInterface
{
    public interface IKycService
    {
        Task<Result<KycUserResponseDto>> SubmitKycService(string userId, KycRequestDto req);
    }
}
