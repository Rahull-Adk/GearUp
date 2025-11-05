using Email.Net;
using GearUp.Infrastructure.Helpers;
using Moq;


namespace GearUp.UnitTests.Infrastructure.Helpers
{
    public class EmailSenderTest
    {
        private readonly Mock<IEmailService> _emailServiceMock;
        private const string ClientUrl = "http://localhost:3000";
        private readonly EmailSender _emailSender;
        public EmailSenderTest()
        {
            _emailServiceMock = new Mock<IEmailService>();
            _emailSender = new EmailSender(_emailServiceMock.Object, "text@gmail.com", "test", null);
        }

        [Fact]
        public async Task SendVerificationEmail_ShouldSendEmail()
        {
            // Arrange
            var email = "test@example.com";
            var token = "abc123";


            _emailServiceMock.Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EmailSendingResult(true, "Email sent successfully"));

            await _emailSender.SendVerificationEmail(email, token);
            _emailServiceMock.Verify(
                   s => s.SendAsync(It.Is<EmailMessage>(m =>
                       m.To.Any(t => t.Address == email) &&
                       m.Subject.Contains("Verify Your Email")),
                       It.IsAny<CancellationToken>()),
                   Times.Once);

        }

        [Fact]
        public async Task SendPasswordResetEmail_Should_Call_SendAsync_Once()
        {
            await _emailSender.SendPasswordResetEmail("reset@example.com", "resettoken");

            _emailServiceMock.Verify(
                s => s.SendAsync(It.Is<EmailMessage>(m =>
                    m.Subject.Contains("Reset Your GearUp Password")), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SendEmailReset_Should_Call_SendAsync_Once()
        {
            await _emailSender.SendEmailReset("reset@example.com", "resettoken");
            _emailServiceMock.Verify(s => s.SendAsync(It.Is<EmailMessage>(m =>
                    m.Subject.Contains("Verify Your New Email Address")), It.IsAny<CancellationToken>()),
                Times.Once);

        }
    }

}
