using System.Text;
using System.Threading.RateLimiting;
using AutoMapper;
using CloudinaryDotNet;
using FluentValidation;
using GearUp.Application.Common;
using GearUp.Application.Interfaces;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.AdminServiceInterface;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.Interfaces.Services.CarServiceInterface;
using GearUp.Application.Interfaces.Services.PostServiceInterface;
using GearUp.Application.Interfaces.Services.UserServiceInterface;
using GearUp.Application.Mappings;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Application.Services;
using GearUp.Application.Services.Admin;
using GearUp.Application.Services.Auth;
using GearUp.Application.Services.Cars;
using GearUp.Application.Services.Posts;
using GearUp.Application.Services.Users;
using GearUp.Application.Validators;
using GearUp.Domain.Entities.Users;
using GearUp.Domain.Enums;
using GearUp.Infrastructure;
using GearUp.Infrastructure.Helpers;
using GearUp.Infrastructure.Persistence;
using GearUp.Infrastructure.Repositories;
using GearUp.Infrastructure.Seed;
using GearUp.Infrastructure.SignalR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

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
            var brevo_api_key = config["BREVO_API_KEY"];
            var fromEmail = config["FromEmail"];
            var clientUrl = config["ClientUrl"];
            var cloudinary_secret = config["CLOUDINARY_URL"];



            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(audience) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(accessToken_SecretKey) || string.IsNullOrEmpty(brevo_api_key) || string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(emailVerificationToken_SecretKey) || string.IsNullOrEmpty(clientUrl))
            {
                throw new InvalidOperationException("Secret keys not found");
            }
            ILogger<EmailSender> logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<EmailSender>();

            services.AddInfrastructure(connectionString, audience, issuer, accessToken_SecretKey, brevo_api_key, fromEmail, emailVerificationToken_SecretKey, clientUrl, logger);


            // Swagger Injection
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();


            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new UserMappingProfile());
                cfg.AddProfile(new KycMappingProfile());
                cfg.AddProfile(new CarMappingProfile());

            }, NullLoggerFactory.Instance);

            mapperConfig.AssertConfigurationIsValid();

            var mapper = mapperConfig.CreateMapper();
            services.AddSingleton(mapper);

            services.AddSignalR();

            // Service Injection
            services.AddScoped<DbSeeder>();
            services.AddScoped<IRegisterService, RegisterService>();
            services.AddScoped<ILoginService, LoginService>();
            services.AddScoped<ILogoutService, LogoutService>();
            services.AddScoped<ICarService, CarService>();
            services.AddScoped<ICarImageService, CarImageService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<ILikeService, LikeService>();
            services.AddScoped<IEmailVerificationService, EmailVerificationService>();
            services.AddSingleton<ICloudinaryImageUploader, CloudinaryImageUploader>();
            services.AddScoped<IGeneralUserService, GeneralUserService>();
            services.AddScoped<IGeneralAdminService, GeneralAdminService>();
            services.AddScoped<IKycService, KycService>();
            services.AddScoped<IProfileUpdateService, ProfileUpdateService>();
            services.AddScoped<IDocumentProcessor, DocumentProcessor>();
            services.AddScoped<IRealTimeNotifier, SignalRRealTimeNotifier>();

            // Redis Cache Injection
            var redisConnection = config["Redis:ConnectionString"] ?? "localhost:6379";
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "GearUpInstance";
            });

            services.AddScoped<ICacheService, CacheService>();

            // API Versioning
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            });

            // Health Checks
            services.AddHealthChecks()
                .AddDbContextCheck<GearUpDbContext>("database")
                .AddRedis(config["Redis:ConnectionString"] ?? "localhost:6379", name: "redis");



            // Repository Injections
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAdminRepository, AdminRepository>();
            services.AddScoped<ICarRepository, CarRepository>();
            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();
            services.AddScoped<ILikeRepository, LikeRepository>();
            services.AddScoped<IViewRepository, ViewRepository>();
            services.AddScoped<ICommonRepository, CommonRepository>();
            services.AddScoped<ITokenRepository, TokenRepository>();

            // Validator Injections
            services.AddScoped<IValidator<RegisterRequestDto>, RegisterRequestDtoValidator>();
            services.AddScoped<IValidator<LoginRequestDto>, LoginRequestDtoValidator>();
            services.AddScoped<IValidator<PasswordResetReqDto>, PasswordResetValidator>();
            services.AddScoped<IValidator<AdminLoginRequestDto>, AdminLoginRequestDtoValidator>();
            services.AddScoped<IValidator<CreateCarRequestDto>, CarRequestDtoValidator>();
            services.AddScoped<IValidator<UpdateCarDto>, UpdateCarDtoValidator>();
            services.AddScoped<IValidator<CreatePostRequestDto>, PostValidators>();

            // Password Hasher Injection
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            services.Configure<Settings>(option =>
            {
                option.EmailVerificationToken_SecretKey = emailVerificationToken_SecretKey;
            });

            // Cloudinary Injection
            Cloudinary cloudinary = new(cloudinary_secret);
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
            .AddPolicy("CustomerOnly", policy => policy.RequireRole(UserRole.Customer.ToString()))
            .AddPolicy("AdminOnly", policy => policy.RequireRole(UserRole.Admin.ToString()))
            .AddPolicy("DealerOnly", policy => policy.RequireRole(UserRole.Dealer.ToString()));


            // CORS Policy
            services.AddCors(opt =>
            {
                opt.AddPolicy("AllowFrontend", builder =>
                {
                    builder.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                    builder.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                });
            });


            // JWT Authentication Injection
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(ops =>
            {
                ops.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];


                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/hubs/post")))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
                ops.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                    NameClaimType = "id",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(accessToken_SecretKey))
                };
            });


        }
    }
}
