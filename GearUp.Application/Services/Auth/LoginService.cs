
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;

namespace GearUp.Application.Services.Auth
{
    public class LoginService : ILoginService
    {
        public Task<Result<string>> LoginUser(string email, string password)
        {
            throw new NotImplementedException();
        }
    }
}
