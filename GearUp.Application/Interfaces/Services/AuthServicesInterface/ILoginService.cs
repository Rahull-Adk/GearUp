
using GearUp.Application.Common;

namespace GearUp.Application.Interfaces.Services.AuthServicesInterface
{
    public interface ILoginService
    {
        Task<Result<string>> LoginUser(string email, string password);
    }
}
