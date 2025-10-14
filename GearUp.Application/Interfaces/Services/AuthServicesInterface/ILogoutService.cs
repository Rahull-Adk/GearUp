namespace GearUp.Application.Interfaces.Services.AuthServicesInterface
{
    public interface ILogoutService
    {
        Task Logout(string refreshToken);
    }
}
