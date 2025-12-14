using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using GearUp.Infrastructure.Helpers;
using Moq;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Model;


namespace GearUp.UnitTests.Infrastructure.Helpers
{
    public class EmailSenderTests
    {
        private readonly EmailSender _emailSender;
        private readonly Mock<ITransactionalEmailClient> _emailClientMock;

        public EmailSenderTests()
        {
            _emailClientMock = new Mock<ITransactionalEmailClient>();

            _emailClientMock
                .Setup(c => c.SendAsync(It.IsAny<SendSmtpEmail>()))
                .ReturnsAsync(new CreateSmtpEmail("123", new List<string> { "321" }));

            _emailSender = new EmailSender(
                _emailClientMock.Object,
                "text@gmail.com",
                "http://localhost:3000",
                null
            );
        }


        [Fact]
        public async System.Threading.Tasks.Task SendVerificationEmail_ShouldSendEmail()
        {
            // Arrange
            var email = "test@example.com";
            var token = "abc123";
            var sendSmtpEmail = new SendSmtpEmail
            {
                To = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(email, "User") }
                ,
                Subject = "sub",
                TextContent = "plainTextContent",
                Sender = new SendSmtpEmailSender { Email = "sender@exmaple", Name = "GearUp Support" },
                HtmlContent = $@""
            };
            _emailClientMock
      .Setup(s => s.SendAsync(
          It.Is<SendSmtpEmail>(email =>
              email.Subject == "sub" &&
              email.TextContent == "plainTextContent" &&
              email.Sender.Email == "sender@exmaple" &&
              email.To.Any(t => t.Email == "test@example.com")
          )))
      .ReturnsAsync(new CreateSmtpEmail("123", new List<string> { "321" }));


            // Act
            await _emailSender.SendVerificationEmail(email, token);

            // Assert
            _emailClientMock.Verify(s => s.SendAsync(It.IsAny<SendSmtpEmail>()), Times.Once);

        }

        [Fact]
        public async System.Threading.Tasks.Task SendPasswordResetEmail_Should_Call_SendAsync_Once()
        {
            // Arrange
            var email = "test@example.com";
            var sendSmtpEmail = new SendSmtpEmail
            {
                To = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(email, "User") }
                ,
                Subject = "sub",
                TextContent = "plainTextContent",
                Sender = new SendSmtpEmailSender { Email = "sender@exmaple", Name = "GearUp Support" },
                HtmlContent = $@""
            };
            _emailClientMock
  .Setup(s => s.SendAsync(
      It.Is<SendSmtpEmail>(email =>
          email.Subject == "sub" &&
          email.TextContent == "plainTextContent" &&
          email.Sender.Email == "sender@exmaple" &&
          email.To.Any(t => t.Email == "test@example.com")
      )))
  .ReturnsAsync(new CreateSmtpEmail("123", new List<string> { "321" }));

            // Act
            await _emailSender.SendPasswordResetEmail("reset@example.com", "resettoken");

            // Assert
            _emailClientMock.Verify(s => s.SendAsync(It.IsAny<SendSmtpEmail>()), Times.Once);

        }

        [Fact]
        public async System.Threading.Tasks.Task SendEmailReset_Should_Call_SendAsync_Once()
        {
            // Arrange
            var email = "test@example.com";
            var sendSmtpEmail = new SendSmtpEmail
            {
                To = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(email, "User") }
                ,
                Subject = "sub",
                TextContent = "plainTextContent",
                Sender = new SendSmtpEmailSender { Email = "sender@exmaple", Name = "GearUp Support" },
                HtmlContent = $@""
            };
            _emailClientMock
 .Setup(s => s.SendAsync(
     It.Is<SendSmtpEmail>(email =>
         email.Subject == "sub" &&
         email.TextContent == "plainTextContent" &&
         email.Sender.Email == "sender@exmaple" &&
         email.To.Any(t => t.Email == "test@example.com")
     )))
 .ReturnsAsync(new CreateSmtpEmail("123", new List<string> { "321" }));

            // Act
            await _emailSender.SendEmailReset("reset@example.com", "resettoken");
            // Assert
            _emailClientMock.Verify(s => s.SendAsync(It.IsAny<SendSmtpEmail>()), Times.Once);

        }
    }


}
