
using GearUp.Application.Common;

namespace GearUp.Application.Interfaces.Services.AuthServicesInterface
{
    public interface IEmailVerificationService
    {
        Task<Result<string>> VerifyEmail(string token);
    }
}
