using GearUp.Application.Common;
using GearUp.Application.ServiceDtos.Auth;
namespace GearUp.Application.Interfaces.Services.AuthServicesInterface
{
    public interface IRegisterService
    {
        public Task<Result<RegisterResponseDto>> RegisterUser(RegisterRequestDto data);
    }
}
