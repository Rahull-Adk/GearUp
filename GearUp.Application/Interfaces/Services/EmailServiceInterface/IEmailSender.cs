
namespace GearUp.Application.Interfaces.Services.EmailServiceInterface
{
    public interface IEmailSender
    {
        Task SendVerificationEmail(string toEmail, string verificationToken);
    }
}
