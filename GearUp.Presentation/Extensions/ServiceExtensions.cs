using AutoMapper;
using Email.Net;
using Email.Net.Channel.SendGrid;
using FluentValidation;
using GearUp.Application.Common;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.Interfaces.Services.UserServiceInterface;
using GearUp.Application.Mappings;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.Services.Auth;
using GearUp.Application.Services.Users;
using GearUp.Application.Validators;
using GearUp.Domain.Entities.Users;
using GearUp.Domain.Enums;
using GearUp.Infrastructure;
using GearUp.Infrastructure.Repositories;
using GearUp.Infrastructure.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using System.Net.Mail;
using System.Text;
using System.Threading.RateLimiting;
using CloudinaryDotNet;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Services.Admin;
using GearUp.Application.Interfaces.Services.AdminServiceInterface;
using StackExchange.Redis;
using GearUp.Application.Services;


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
            var cloudinary_secret = config["CLOUDINARY_URL"];



            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(audience) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(accessToken_SecretKey) || string.IsNullOrEmpty(sendGridKey) || string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(emailVerificationToken_SecretKey) || string.IsNullOrEmpty(clientUrl))
            {
                throw new InvalidOperationException("Secret keys not found");
            }
            ILogger<EmailSender> logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<EmailSender>();

            services.AddInfrastructure(connectionString, audience, issuer, accessToken_SecretKey, sendGridKey, fromEmail, emailVerificationToken_SecretKey, clientUrl, logger);


            // Swagger Injection
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();


            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new UserMappingProfile());
                cfg.AddProfile(new KycMappingProfile());
            }, NullLoggerFactory.Instance);

            mapperConfig.AssertConfigurationIsValid();

            var mapper = mapperConfig.CreateMapper();
            services.AddSingleton(mapper);

            // Service Injection
            services.AddScoped<IRegisterService, RegisterService>();
            services.AddScoped<ILoginService, LoginService>();
            services.AddScoped<ILogoutService, LogoutService>();
            services.AddScoped<IEmailVerificationService, EmailVerificationService>();
            services.AddSingleton<ICloudinaryImageUploader, CloudinaryImageUploader>();
            services.AddScoped<IGeneralUserService, GeneralUserService>();
            services.AddScoped<IGeneralAdminService, GeneralAdminService>();
            services.AddScoped<IKycService, KycService>();
            services.AddScoped<IProfileUpdateService, ProfileUpdateService>();
            services.AddScoped<IDocumentProcessor, DocumentProcessor>();

            // Redis Cache Injection
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = "localhost:6379";
                options.InstanceName = "GearUpInstance";
            });

            services.AddScoped<ICacheService, CacheService>();

            // Repository Injections
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITokenRepository, TokenRepository>();
            services.AddScoped<IAdminRepository, AdminRepository>();

            // Validator Injections
            services.AddScoped<IValidator<RegisterRequestDto>, RegisterRequestDtoValidator>();
            services.AddScoped<IValidator<LoginRequestDto>, LoginRequestDtoValidator>();
            services.AddScoped<IValidator<PasswordResetReqDto>, PasswordResetValidator>();
            services.AddScoped<IValidator<AdminLoginRequestDto>, AdminLoginRequestDtoValidator>();

            // Password Hasher Injection
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            services.Configure<Settings>(option =>
            {
                option.EmailVerificationToken_SecretKey = emailVerificationToken_SecretKey;
            });

            // Cloudinary Injection
            Cloudinary cloudinary = new Cloudinary(cloudinary_secret);
            cloudinary.Api.Secure = true;
            services.AddSingleton(cloudinary);

            //Rate Limiting
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy("Fixed", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: key => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));
            });

            //Role Base Policies
            services.AddAuthorizationBuilder()
            .AddPolicy("CustomerOnly", policy => policy.RequireRole(UserRole.Customer.ToString()));
            services.AddAuthorizationBuilder()
                 .AddPolicy("AdminOnly", policy => policy.RequireRole(UserRole.Admin.ToString()));
         
            // CORS Policy
            services.AddCors(opt =>
            {
                opt.AddPolicy("AllowFrontend", builder =>
                {
                    builder.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                });
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
                RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                NameClaimType = "id",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(accessToken_SecretKey))
            });


        }
    }
}
