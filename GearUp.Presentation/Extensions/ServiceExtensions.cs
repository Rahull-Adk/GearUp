using Email.Net;
using Email.Net.Channel.SendGrid;
using FluentValidation;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.Interfaces.Services.EmailServiceInterface;
using GearUp.Application.Interfaces.Services.JwtServiceInterface;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.Services.Auth;
using GearUp.Application.Validators;
using GearUp.Domain.Entities.Users;
using GearUp.Infrastructure;
using GearUp.Infrastructure.JwtServices;
using GearUp.Infrastructure.Repositories;
using GearUp.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
            services.AddDbContext<GearUpDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            // Swagger Injection
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // Service Injections
            services.AddSingleton<ITokenGenerator, TokenGenerator>();
            services.AddScoped<IRegisterService, RegisterService>();
            services.AddScoped<ILoginService, LoginService>();
           
            services.Configure<JwtSetting>(config.GetSection("Jwt"));
            var jwt = config.GetSection("Jwt").Get<JwtSetting>();

            // Repository Injections
            services.AddScoped<IUserRepository, UserRepository>();

            // Validator Injections
            services.AddScoped<IValidator<RegisterRequestDto>, RegisterRequestDtoValidator>();

            // Password Hasher Injection
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

            //Rate Limiting
            services.AddRateLimiter(options =>
            {
                options.AddPolicy("Fixed", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: key => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 2,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 2
                        }));
            });

            // Email Service Injection

            services.AddScoped<IEmailSender, EmailSender>();    
            var sendGridKey = Environment.GetEnvironmentVariable("SendGridApi");
            var fromEmail = Environment.GetEnvironmentVariable("FromEmail");
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
                ValidIssuer = jwt!.Issuer,
                ValidAudience = jwt!.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt!.AcessToken_SecretKey))
            });


            
        }
    }
}
