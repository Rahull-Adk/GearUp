using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.Auth;
namespace GearUp.Application.Interfaces.Services.AuthServicesInterface
{
    public interface ILoginService
    {
        Task<Result<LoginResponseDto>> LoginUser(LoginRequestDto req);
        Task<Result<LoginResponseDto>> RotateRefreshToken(string refreshToken);
        Task<Result<string>> SendPasswordResetToken(string email);
        Task<Result<string>> ResetPassword(string token, PasswordResetReqDto req);
    }
}
