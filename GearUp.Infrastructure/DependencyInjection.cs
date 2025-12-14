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
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, string audience, string issuer, string accessToken_SecretKey, string brevo_api_key, string fromEmail, string emailVerificationToken_SecretKey, string clientUrl, ILogger<EmailSender> logger)
        {
            services.AddDbContext<GearUpDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            services.AddSingleton<ITokenGenerator>(new TokenGenerator(accessToken_SecretKey, audience, issuer, emailVerificationToken_SecretKey));

            services.AddSingleton<ITokenValidator>(new TokenValidator(audience, issuer));

            services.AddSingleton<ITransactionalEmailClient>(new BrevoEmailClient(brevo_api_key));

            services.AddScoped<IEmailSender>(provider => new EmailSender(provider.GetRequiredService<ITransactionalEmailClient>(), fromEmail, clientUrl, logger));

            return services;
        }
    }
}
