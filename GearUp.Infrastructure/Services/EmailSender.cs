using Email.Net;
using GearUp.Application.Interfaces.Services.EmailServiceInterface;

namespace GearUp.Infrastructure.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IEmailService _emailService;
        public EmailSender(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task SendVerificationEmail(string toEmail, string verificationToken)
        {
            var fromEmail = Environment.GetEnvironmentVariable("FromEmail");
            var message = EmailMessage.Compose()
         .From(fromEmail, "GearUp")
         .To(toEmail)
         .WithSubject("Verify Your Email — Welcome to GearUp!")
         .WithPlainTextContent("Welcome to GearUp! Please verify your email by clicking the link below.")
         .WithHtmlContent($@"
<html>
<body style='margin:0; padding:0; background-color:#f4f6f8; font-family:Segoe UI, Roboto, Helvetica, Arial, sans-serif;'>
    <table role='presentation' cellpadding='0' cellspacing='0' width='100%'>
        <tr>
            <td align='center' style='padding:40px 0;'>
                <table role='presentation' width='600' cellpadding='0' cellspacing='0' style='background:#ffffff; border-radius:10px; box-shadow:0 3px 12px rgba(0,0,0,0.08); overflow:hidden;'>
                    <tr>
                        <td style='background-color:#0078D7; padding:20px 40px; text-align:center;'>
                            <h1 style='color:#ffffff; font-size:26px; margin:0;'>GearUp</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding:40px; text-align:center;'>
                            <h2 style='color:#222; font-size:22px; margin-bottom:16px;'>Verify Your Email Address</h2>
                            <p style='color:#555; font-size:15px; line-height:1.6; margin:0 0 30px 0;'>
                                Welcome to <strong>GearUp</strong>! We’re excited to have you on board.<br />
                                Please confirm your email address to activate your account.
                            </p>
                            <a href='https://e61882394d53.ngrok-free.app/api/v1/auth/verify?token={verificationToken}'
                               style='display:inline-block; background-color:#0078D7; color:#ffffff; text-decoration:none;
                                      padding:14px 32px; border-radius:8px; font-size:16px; font-weight:600;'>
                                Verify Email
                            </a>
                            <p style='color:#888; font-size:13px; margin-top:32px;'>
                                This link will expire in 24 hours for your security.
                            </p>
                            <hr style='border:none; border-top:1px solid #eee; margin:40px 0;' />
                            <p style='color:#777; font-size:13px; line-height:1.6;'>
                                If you didn’t create an account, please ignore this message or contact our support team.
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style='background-color:#f8f9fb; padding:20px 40px; text-align:center;'>
                            <p style='color:#aaa; font-size:12px; margin:0;'>
                                © {DateTime.UtcNow.Year} GearUp. All rights reserved.<br/>
                                <a href='https://gearup.com' style='color:#0078D7; text-decoration:none;'>gearup.com</a>
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>")
         .WithNormalPriority()
         .Build();


            await _emailService.SendAsync(message);
        }
    }
}
