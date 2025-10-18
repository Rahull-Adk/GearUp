using Email.Net;
using Email.Net.Channel.SendGrid;
using FluentValidation;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.Services.Auth;
using GearUp.Application.Validators;
using GearUp.Domain.Entities.Users;
using GearUp.Infrastructure;
using GearUp.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net.Mail;
using System.Text;
using System.Threading.RateLimiting;

namespace GearUp.Presentation.Extensions
{
    public static class ServiceExtensions
    {
        public static void AddServices(this IServiceCollection services, IConfiguration config)
        {

            // DbContext Injection
            var connectionString = config.GetConnectionString("DefaultConnection");
            var audience = config["Jwt:Audience"];
            var issuer = config["Jwt:Issuer"];
            var accessToken_SecretKey = config["Jwt:AccessToken_SecretKey"];
            var emailVerificationToken_SecretKey = config["Jwt:EmailVerificationToken_SecretKey"];
            var sendGridKey = config["SendGridApiKey"];
            var fromEmail = config["FromEmail"];
            var clientUrl = config["ClientUrl"];

            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(audience) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(accessToken_SecretKey) || string.IsNullOrEmpty(sendGridKey) || string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(emailVerificationToken_SecretKey) || string.IsNullOrEmpty(clientUrl))
            {
                throw new InvalidOperationException("Secret keys not found");
            }
            services.AddInfrastructure(connectionString, audience, issuer, accessToken_SecretKey, sendGridKey, fromEmail, emailVerificationToken_SecretKey, clientUrl);


            // Swagger Injection
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // Service Injections

            services.AddScoped<IRegisterService, RegisterService>();
            services.AddScoped<ILoginService, LoginService>();
            services.AddScoped<ILogoutService, LogoutService>();
            services.AddScoped<IEmailVerificationService, EmailVerificationService>();

            // Repository Injections
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITokenRepository, TokenRepository>();

            // Validator Injections
            services.AddScoped<IValidator<RegisterRequestDto>, RegisterRequestDtoValidator>();
            services.AddScoped<IValidator<LoginRequestDto>, LoginRequestDtoValidator>();
            services.AddScoped<IValidator<PasswordResetReqDto>, PasswordResetValidator>();

            // Password Hasher Injection
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            services.Configure<Settings>(option =>
            {
                option.EmailVerificationToken_SecretKey = emailVerificationToken_SecretKey;
            });
            //Rate Limiting
            services.AddRateLimiter(options =>
            {
                options.AddPolicy("Fixed", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: key => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 60,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 2
                        }));
            });

            // Email Service Injection

            services.AddEmailNet(options =>
            {
                options.PauseSending = false;
                options.DefaultFrom = new MailAddress(fromEmail);
                options.DefaultEmailDeliveryChannel = SendgridEmailDeliveryChannel.Name;
            }).UseSendGrid(sendGridKey);

            // JWT Authentication Injection
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(ops => ops.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                RoleClaimType = "role",
                NameClaimType = "id",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(accessToken_SecretKey))
            });
            
           
        }
    }
}
