using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Infrastructure.Helpers;
using GearUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GearUp.Infrastructure
{
    public static class DependencyInjection
    {
        private const int DbContextPoolSize = 128;
        private const int DbCommandTimeoutSeconds = 60;
        private const int MaxRetryCount = 5;
        private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(10);

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, string audience, string issuer, string accessToken_SecretKey, string brevo_api_key, string fromEmail, string emailVerificationToken_SecretKey, string clientUrl, string opaqueTokenPepper, ILogger<EmailSender> logger)
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

            services.AddScoped<IEmailSender>(provider => new EmailSender(provider.GetRequiredService<ITransactionalEmailClient>(), fromEmail, clientUrl, logger));

            return services;
        }
    }
}
