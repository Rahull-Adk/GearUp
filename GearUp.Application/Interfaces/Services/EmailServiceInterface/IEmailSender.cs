
namespace GearUp.Application.Interfaces.Services.EmailServiceInterface
{
    public interface IEmailSender
    {
        Task SendVerificationEmail(string toEmail, string verificationToken);
        Task SendPasswordResetEmail(string toEmail, string resetToken);
        Task SendEmailReset(string toEmail, string resetToken);
    }
}
