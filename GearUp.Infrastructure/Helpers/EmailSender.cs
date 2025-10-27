using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using Email.Net;

namespace GearUp.Infrastructure.Helpers
{
    public class EmailSender : IEmailSender
    {
        private readonly IEmailService _emailService;
        private readonly string _fromEmail;
        private readonly string _clientUrl;

        public EmailSender(IEmailService emailService, string fromEmail, string clientUrl)
        {
            _emailService = emailService;
            _fromEmail = fromEmail;
            _clientUrl = clientUrl;
        }

        public async Task SendVerificationEmail(string toEmail, string verificationToken)
        {
            var verifyLink = $"{_clientUrl}/verify?token={verificationToken}";
            await SendEmailAsync(
                toEmail,
                "Verify Your Email — Welcome to GearUp!",
                "Welcome to GearUp! Please verify your email by clicking the link below.",
                "Verify Your Email Address",
                "Welcome to GearUp! We’re excited to have you on board. Please confirm your email address to activate your account.",
                "Verify Email",
                verifyLink
            );
        }

        public async Task SendPasswordResetEmail(string toEmail, string resetToken)
        {
            var resetLink = $"{_clientUrl}/reset-password?token={resetToken}";
            await SendEmailAsync(
                toEmail,
                "Reset Your GearUp Password",
                "We received a request to reset your GearUp password. Click the link below.",
                "Reset Your Password",
                "We received a request to reset your GearUp account password. Click the button below to set a new password.",
                "Reset Password",
                resetLink
            );
        }

        public async Task SendEmailReset(string toEmail, string resetToken)
        {
            var resetLink = $"{_clientUrl}/reset-email?token={resetToken}";

            await SendEmailAsync(
                toEmail,
                "Verify Your New Email Address - GearUp",
                "We’ve received a request to update your email address on GearUp.", 
                "Verify Your New Email Address",
                @"You recently requested to change the email address associated with your GearUp account. 
        To complete this update and keep your account secure, please verify your new email by clicking the button below.",
                "Verify Email",
                resetLink
            );
        }

        private async Task SendEmailAsync(
            string toEmail,
            string subject,
            string plainTextContent,
            string heading,
            string bodyText,
            string buttonText,
            string buttonLink
        )
        {
            var message = EmailMessage.Compose()
                .From(_fromEmail, "GearUp Support")
                .To(toEmail)
                .WithSubject(subject)
                .WithPlainTextContent(plainTextContent)
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
                            <h2 style='color:#222; font-size:22px; margin-bottom:16px;'>{heading}</h2>
                            <p style='color:#555; font-size:15px; line-height:1.6; margin:0 0 30px 0;'>{bodyText}</p>
                            <a href='{buttonLink}'
                               style='display:inline-block; background-color:#0078D7; color:#ffffff; text-decoration:none;
                                      padding:14px 32px; border-radius:8px; font-size:16px; font-weight:600;'>{buttonText}</a>
                            <p style='color:#888; font-size:13px; margin-top:32px;'>This link will expire in 24 hours for your security.</p>
                            <hr style='border:none; border-top:1px solid #eee; margin:40px 0;' />
                            <p style='color:#777; font-size:13px; line-height:1.6;'>
                                If you didn’t request this, please ignore this email or contact our support team.
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
