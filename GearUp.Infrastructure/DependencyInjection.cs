using Email.Net;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GearUp.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, string audience, string issuer, string accessToken_SecretKey, string sendGridKey, string fromEmail, string emailVerificationToken_SecretKey, string clientUrl)
        {
            services.AddDbContext<GearUpDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            services.AddSingleton<ITokenGenerator>(new TokenGenerator(accessToken_SecretKey, audience, issuer, emailVerificationToken_SecretKey));

            services.AddSingleton<ITokenValidator>(new TokenValidator(audience, issuer));

            services.AddScoped<IEmailSender>(provider => new EmailSender(provider.GetRequiredService<IEmailService>(), fromEmail, clientUrl));

            return services;
        }
    }
}
