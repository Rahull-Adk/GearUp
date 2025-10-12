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

        public async Task SendVerificationEmail(string toEmail, string  verificationToken)
        {
            var fromEmail = Environment.GetEnvironmentVariable("FromEmail");
            var message = EmailMessage.Compose()
     .From(fromEmail)
     .To(toEmail)
     .WithSubject("GearUp | Verify Your Email")
     .WithPlainTextContent("Please verify your email by clicking the link below.")
     .WithHtmlContent($@"
        <html>
        <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 40px;'>
            <div style='max-width: 600px; margin: auto; background-color: #ffffff; border-radius: 10px; box-shadow: 0 4px 10px rgba(0,0,0,0.1); padding: 30px; text-align: center;'>
                <h2 style='color: #333;'>Welcome to <span style='color: #0078D7;'>GearUp</span>!</h2>
                <p style='font-size: 16px; color: #555;'>Thanks for signing up. To get started, please verify your email address by clicking the button below:</p>
                
                <a href='https://localhost:7083/verify?token={verificationToken}'
                   style='display: inline-block; background-color: #0078D7; color: white; text-decoration: none; 
                          padding: 12px 24px; border-radius: 6px; font-size: 16px; font-weight: bold; margin-top: 20px;'>
                    Verify Email
                </a>
                
                <p style='font-size: 14px; color: #888; margin-top: 30px;'>
                    If you didn’t create an account, you can safely ignore this email.
                </p>
                <hr style='margin: 30px 0; border: none; border-top: 1px solid #eee;' />
                <p style='font-size: 12px; color: #aaa;'>© {DateTime.UtcNow.Year} GearUp. All rights reserved.</p>
            </div>
        </body>
        </html>")
     .WithNormalPriority()
     .Build();


            await _emailService.SendAsync(message);
        }
    }
}
