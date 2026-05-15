using GearUp.Application.Interfaces.Messaging;
using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Infrastructure.Helpers;
using GearUp.Infrastructure.Messaging;
using GearUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace GearUp.Infrastructure
{
    public static class DependencyInjection
    {
        private const int DbContextPoolSize = 128;
        private const int DbCommandTimeoutSeconds = 60;
        private const int MaxRetryCount = 3;
        private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(2);

        private static Uri BuildRabbitMqUri(RabbitMqOptions options)
        {
            var virtualHost = options.VirtualHost == "/" ? string.Empty : "/" + options.VirtualHost.TrimStart('/');
            return new Uri($"amqp://{Uri.EscapeDataString(options.Username)}:{Uri.EscapeDataString(options.Password)}@{options.Host}:{options.Port}{virtualHost}");
        }

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, string audience, string issuer, string accessToken_SecretKey, string brevo_api_key, string fromEmail, string emailVerificationToken_SecretKey, string clientUrl, string opaqueTokenPepper, ILogger<EmailSender> logger, RabbitMqOptions rabbitMqOptions)
        {
            services.AddDbContextPool<GearUpDbContext>(
                options =>
                    options.UseNpgsql(
                        connectionString,
                        npgsqlOptions =>
                        {
                            npgsqlOptions.EnableRetryOnFailure(
                                maxRetryCount: MaxRetryCount,
                                maxRetryDelay: MaxRetryDelay,
                                errorCodesToAdd: null);
                            npgsqlOptions.CommandTimeout(DbCommandTimeoutSeconds);
                        }),
                poolSize: DbContextPoolSize);

            services.AddSingleton<ITokenGenerator>(new TokenGenerator(accessToken_SecretKey, audience, issuer, emailVerificationToken_SecretKey, opaqueTokenPepper));

            services.AddSingleton<ITokenValidator>(new TokenValidator(audience, issuer));

            services.AddSingleton<ITransactionalEmailClient>(new BrevoEmailClient(brevo_api_key));

            services.AddSingleton<IEmailSender>(provider => new EmailSender(provider.GetRequiredService<ITransactionalEmailClient>(), fromEmail, clientUrl, logger));

            services.AddSingleton(rabbitMqOptions);

            if (!rabbitMqOptions.UseRabbitMQ)
            {
                services.AddSingleton<IMessagePublisher, InMemoryPublisher>();
                return services;
            }

            services.AddSingleton<IConnection>(_ =>
            {
                var factory = new ConnectionFactory
                {
                    Uri = BuildRabbitMqUri(rabbitMqOptions),
                    AutomaticRecoveryEnabled = true
                };

                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });

            services.AddSingleton<IChannel>(sp =>
            {
                var connection = sp.GetRequiredService<IConnection>();
                var channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

                channel.ExchangeDeclareAsync(rabbitMqOptions.Exchange, ExchangeType.Topic, durable: true).GetAwaiter().GetResult();
                
                // DLQ Setup
                var dlqExchange = "gearup.dlx";
                channel.ExchangeDeclareAsync(dlqExchange, ExchangeType.Direct, durable: true).GetAwaiter().GetResult();
                channel.QueueDeclareAsync(queue: rabbitMqOptions.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false).GetAwaiter().GetResult();
                channel.QueueBindAsync(queue: rabbitMqOptions.DeadLetterQueue, exchange: dlqExchange, routingKey: "dead-letter").GetAwaiter().GetResult();

                // Retry Setup
                channel.ExchangeDeclareAsync(rabbitMqOptions.RetryExchange, ExchangeType.Fanout, durable: true).GetAwaiter().GetResult();
                var retryArgs = new Dictionary<string, object?>
                {
                    { "x-message-ttl", rabbitMqOptions.RetryDelayMs },
                    { "x-dead-letter-exchange", rabbitMqOptions.Exchange }
                };
                channel.QueueDeclareAsync(queue: rabbitMqOptions.RetryQueue, durable: true, exclusive: false, autoDelete: false, arguments: retryArgs).GetAwaiter().GetResult();
                channel.QueueBindAsync(queue: rabbitMqOptions.RetryQueue, exchange: rabbitMqOptions.RetryExchange, routingKey: "").GetAwaiter().GetResult();

                var mainQueueArgs = new Dictionary<string, object?>
                {
                    { "x-dead-letter-exchange", dlqExchange },
                    { "x-dead-letter-routing-key", "dead-letter" }
                };

                // Email Queue
                channel.QueueDeclareAsync(queue: rabbitMqOptions.EmailQueue, durable: true, exclusive: false, autoDelete: false, arguments: mainQueueArgs).GetAwaiter().GetResult();
                channel.QueueBindAsync(queue: rabbitMqOptions.EmailQueue, exchange: rabbitMqOptions.Exchange, routingKey: rabbitMqOptions.EmailQueue).GetAwaiter().GetResult();

                // Notification Queue
                channel.QueueDeclareAsync(queue: rabbitMqOptions.NotificationQueue, durable: true, exclusive: false, autoDelete: false, arguments: mainQueueArgs).GetAwaiter().GetResult();
                channel.QueueBindAsync(queue: rabbitMqOptions.NotificationQueue, exchange: rabbitMqOptions.Exchange, routingKey: rabbitMqOptions.NotificationQueue).GetAwaiter().GetResult();

                // Image Processing Queue
                channel.QueueDeclareAsync(queue: rabbitMqOptions.ImageProcessingQueue, durable: true, exclusive: false, autoDelete: false, arguments: mainQueueArgs).GetAwaiter().GetResult();
                channel.QueueBindAsync(queue: rabbitMqOptions.ImageProcessingQueue, exchange: rabbitMqOptions.Exchange, routingKey: rabbitMqOptions.ImageProcessingQueue).GetAwaiter().GetResult();

                // Image Upload Queue
                channel.QueueDeclareAsync(queue: rabbitMqOptions.ImageUploadQueue, durable: true, exclusive: false, autoDelete: false, arguments: mainQueueArgs).GetAwaiter().GetResult();
                channel.QueueBindAsync(queue: rabbitMqOptions.ImageUploadQueue, exchange: rabbitMqOptions.Exchange, routingKey: rabbitMqOptions.ImageUploadQueue).GetAwaiter().GetResult();

                return channel;
            });

            services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

            return services;
        }
    }
}
