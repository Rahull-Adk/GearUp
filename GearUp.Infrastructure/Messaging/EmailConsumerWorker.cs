using System.Text.Json;
using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using GearUp.Application.Messaging.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GearUp.Infrastructure.Messaging
{
    public class EmailConsumerWorker : BackgroundService
    {
        private readonly IChannel _channel;
        private readonly ILogger<EmailConsumerWorker> _logger;
        private readonly IEmailSender _emailSender;
        private readonly RabbitMqOptions _rabbitMqOptions;

        public EmailConsumerWorker(IChannel channel, ILogger<EmailConsumerWorker> logger, IEmailSender emailSender, RabbitMqOptions rabbitMqOptions)
        {
            _channel = channel;
            _logger = logger;
            _emailSender = emailSender;
            _rabbitMqOptions = rabbitMqOptions;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = JsonSerializer.Deserialize<EmailRequestMessage>(body);

                try
                {
                    _logger.LogInformation("Processing email {CorrelationId}", message?.CorrelationId);

                    await HandleEmail(message!);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                }
                catch (Exception e)
                {
                    await RabbitMqRetryHelper.HandleMessageFailureAsync(_channel, ea, _rabbitMqOptions, _logger, e, stoppingToken);
                }
            };
            await _channel.BasicConsumeAsync(_rabbitMqOptions.EmailQueue, false, consumer, cancellationToken: stoppingToken);
        }

        private async Task HandleEmail(EmailRequestMessage message)
        {
            switch (message.TemplateName)
            {
                case "VerifyEmail":
                    await _emailSender.SendVerificationEmail(
                        message.ToEmail,
                        message.Payload["token"]);
                    break;

                case "ResetPassword":
                    await _emailSender.SendPasswordResetEmail(
                        message.ToEmail,
                        message.Payload["token"]);
                    break;

                case "ResetEmail":
                    await _emailSender.SendEmailReset(
                        message.ToEmail,
                        message.Payload["token"]);
                    break;

                default:
                    throw new Exception($"Unknown template: {message.TemplateName}");
            }
        }
    }
}