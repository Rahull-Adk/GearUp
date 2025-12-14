using sib_api_v3_sdk.Model;

namespace GearUp.Application.Interfaces.Services.EmailServiceInterface
{
    public interface ITransactionalEmailClient
    {
        Task<CreateSmtpEmail> SendAsync(SendSmtpEmail email);
    }
}
