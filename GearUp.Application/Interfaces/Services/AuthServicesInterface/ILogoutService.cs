using GearUp.Application.Common;

namespace GearUp.Application.Interfaces.Services.AuthServicesInterface
{
    public interface ILogoutService
    {
        Task<Result<string>> Logout(string refreshToken);
    }
}
