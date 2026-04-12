using System.Text;
using System.Threading.RateLimiting;
using CloudinaryDotNet;
using FluentValidation;
using GearUp.Application.Common;
using GearUp.Application.Interfaces;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services;
using GearUp.Application.Interfaces.Services.AdminServiceInterface;
using GearUp.Application.Interfaces.Services.AppointmentServiceInterface;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.Interfaces.Services.CarServiceInterface;
using GearUp.Application.Interfaces.Services.MessageServiceInterface;
using GearUp.Application.Interfaces.Services.PostServiceInterface;
using GearUp.Application.Interfaces.Services.ReviewServiceInterface;
using GearUp.Application.Interfaces.Services.UserServiceInterface;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.ServiceDtos.Car;
using GearUp.Application.ServiceDtos.Post;
using GearUp.Application.Services;
using GearUp.Application.Services.Admin;
using GearUp.Application.Services.Appointments;
using GearUp.Application.Services.Auth;
using GearUp.Application.Services.Cars;
using GearUp.Application.Services.Messages;
using GearUp.Application.Services.Notifications;
using GearUp.Application.Services.Posts;
using GearUp.Application.Services.Reviews;
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
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Security.Claims;
using StackExchange.Redis;

namespace GearUp.Presentation.Extensions
{
    public static class ServiceExtensions
    {
        private static string? ReadSetting(IConfiguration config, params string[] keys)
        {
            foreach (var key in keys)
            {
                var value = config[key];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        private static string[] BuildAllowedOrigins(IConfiguration config, string requiredClientUrl)
        {
            var configuredOrigins = ReadSetting(config, "Cors:AllowedOrigins", "Cors__AllowedOrigins", "CORS_ALLOWED_ORIGINS");
            var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                requiredClientUrl.TrimEnd('/')
            };

            if (!string.IsNullOrWhiteSpace(configuredOrigins))
            {
                foreach (var origin in configuredOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    origins.Add(origin.TrimEnd('/'));
                }
            }

            return origins.ToArray();
        }

        private static string ResolveRedisConnectionString(IConfiguration config)
        {
            var configuredRedis = ReadSetting(config, "Redis:ConnectionString", "Redis__ConnectionString", "REDIS_URL");

            if (string.IsNullOrWhiteSpace(configuredRedis))
            {
                throw new InvalidOperationException("Redis connection string is not configured. Set Redis:ConnectionString, Redis__ConnectionString, or REDIS_URL.");
            }

            var value = configuredRedis.Trim();
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                !(uri.Scheme.Equals("redis", StringComparison.OrdinalIgnoreCase) ||
                  uri.Scheme.Equals("rediss", StringComparison.OrdinalIgnoreCase)))
            {
                return value;
            }

            var options = new ConfigurationOptions
            {
                Ssl = uri.Scheme.Equals("rediss", StringComparison.OrdinalIgnoreCase),
                AbortOnConnectFail = false
            };

            var port = uri.IsDefaultPort ? 6379 : uri.Port;
            options.EndPoints.Add(uri.Host, port);

            if (!string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                var parts = uri.UserInfo.Split(':', 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2)
                {
                    if (!string.IsNullOrWhiteSpace(parts[0])) options.User = Uri.UnescapeDataString(parts[0]);
                    if (!string.IsNullOrWhiteSpace(parts[1])) options.Password = Uri.UnescapeDataString(parts[1]);
                }
                else if (parts.Length == 1)
                {
                    options.Password = Uri.UnescapeDataString(parts[0]);
                }
            }

            return options.ToString();
        }

        public static void AddServices(this IServiceCollection services, IConfiguration config)
        {
            // DbContext Injection
            var connectionString = ReadSetting(config, "ConnectionStrings:DefaultConnection", "ConnectionStrings__DefaultConnection");
            var audience = ReadSetting(config, "Jwt:Audience", "Jwt__Audience");
            var issuer = ReadSetting(config, "Jwt:Issuer", "Jwt__Issuer");
            var accessTokenSecretKey = ReadSetting(config, "Jwt:AccessToken_SecretKey", "Jwt__AccessToken_SecretKey");
            var emailVerificationTokenSecretKey = ReadSetting(config, "Jwt:EmailVerificationToken_SecretKey", "Jwt__EmailVerificationToken_SecretKey");
            var opaqueTokenPepper = ReadSetting(config, "Jwt:OpaqueTokenPepper", "Jwt__OpaqueTokenPepper", "OpaqueTokenPepper", "OPAQUE_TOKEN_PEPPER");
            var brevoApiKey = ReadSetting(config, "BREVO_API_KEY", "SendGridApiKey");
            var fromEmail = ReadSetting(config, "FromEmail");
            var clientUrl = ReadSetting(config, "ClientUrl");
            var cloudinarySecret = ReadSetting(config, "CLOUDINARY_URL");

            // Backward-compatible fallback for environments that predate explicit pepper configuration.
            opaqueTokenPepper ??= accessTokenSecretKey;

            var missingSettings = new List<string>();
            if (string.IsNullOrWhiteSpace(connectionString)) missingSettings.Add("ConnectionStrings:DefaultConnection");
            if (string.IsNullOrWhiteSpace(audience)) missingSettings.Add("Jwt:Audience");
            if (string.IsNullOrWhiteSpace(issuer)) missingSettings.Add("Jwt:Issuer");
            if (string.IsNullOrWhiteSpace(accessTokenSecretKey)) missingSettings.Add("Jwt:AccessToken_SecretKey");
            if (string.IsNullOrWhiteSpace(emailVerificationTokenSecretKey)) missingSettings.Add("Jwt:EmailVerificationToken_SecretKey");
            if (string.IsNullOrWhiteSpace(opaqueTokenPepper)) missingSettings.Add("Jwt:OpaqueTokenPepper");
            if (string.IsNullOrWhiteSpace(brevoApiKey)) missingSettings.Add("BREVO_API_KEY|SendGridApiKey");
            if (string.IsNullOrWhiteSpace(fromEmail)) missingSettings.Add("FromEmail");
            if (string.IsNullOrWhiteSpace(clientUrl)) missingSettings.Add("ClientUrl");
            if (string.IsNullOrWhiteSpace(cloudinarySecret)) missingSettings.Add("CLOUDINARY_URL");

            if (missingSettings.Count > 0)
            {
                throw new InvalidOperationException($"Missing required configuration values: {string.Join(", ", missingSettings)}");
            }

            var requiredConnectionString = connectionString!;
            var requiredAudience = audience!;
            var requiredIssuer = issuer!;
            var requiredAccessTokenSecretKey = accessTokenSecretKey!;
            var requiredEmailVerificationTokenSecretKey = emailVerificationTokenSecretKey!;
            var requiredOpaqueTokenPepper = opaqueTokenPepper!;
            var requiredBrevoApiKey = brevoApiKey!;
            var requiredFromEmail = fromEmail!;
            var requiredClientUrl = clientUrl!;
            var requiredCloudinarySecret = cloudinarySecret!;

            ILogger<EmailSender> logger = LoggerFactory.Create(builder => builder.AddConsole())
                .CreateLogger<EmailSender>();

            services.AddInfrastructure(requiredConnectionString, requiredAudience, requiredIssuer,
                requiredAccessTokenSecretKey, requiredBrevoApiKey, requiredFromEmail,
                requiredEmailVerificationTokenSecretKey, requiredClientUrl, requiredOpaqueTokenPepper, logger);


            // Swagger Injection
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();


            services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
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
            services.AddScoped<IAppointmentService, AppointmentService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<INotificationService, NotificationService>();


            // Redis Cache Injection

            var redisConnection = ResolveRedisConnectionString(config);

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
                .AddRedis(redisConnection, name: "redis");


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
            services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            services.AddScoped<IReviewRepository, ReviewRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();

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
                option.EmailVerificationToken_SecretKey = requiredEmailVerificationTokenSecretKey;
            });

            // Cloudinary Injection
            Cloudinary cloudinary = new(requiredCloudinarySecret) { Api = { Secure = true } };
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
                .AddPolicy("CustomerOnly", policy => policy.RequireRole(nameof(UserRole.Customer)))
                .AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(UserRole.Admin)))
                .AddPolicy("DealerOnly", policy => policy.RequireRole(nameof(UserRole.Dealer)));

            // OpenTelemetry
            services.AddOpenTelemetry()
                .ConfigureResource(r => r.AddService("GearUp"))
                .WithTracing(t => t
                    .AddSource("GearUp.Auth")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddOtlpExporter())
                .WithMetrics(m => m
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter());

            // CORS Policy
            var allowedOrigins = BuildAllowedOrigins(config, requiredClientUrl);
            services.AddCors(opt =>
            {
                opt.AddPolicy("AllowFrontend", builder =>
                {
                    builder.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
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
                            (path.StartsWithSegments("/hubs/post") || path.StartsWithSegments("/hubs/notification") || path.StartsWithSegments("/hubs/chat")))
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
                    ValidIssuer = requiredIssuer,
                    ValidAudience = requiredAudience,
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = "id",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(requiredAccessTokenSecretKey))
                };
            });
        }
    }
}