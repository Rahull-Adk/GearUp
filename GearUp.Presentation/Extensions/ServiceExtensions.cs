using FluentValidation;
using GearUp.Application.Interfaces.Repositories;
using GearUp.Application.Interfaces.Services.AuthServicesInterface;
using GearUp.Application.ServiceDtos.Auth;
using GearUp.Application.Services.Auth;
using GearUp.Application.Validators;
using GearUp.Domain.Entities.Users;
using GearUp.Infrastructure;
using GearUp.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
            services.AddScoped<IRegisterService, RegisterService>();
           

            // Repository Injections
            services.AddScoped<IUserRepository, UserRepository>();

            // Validator Injections
            services.AddScoped<IValidator<RegisterRequestDto>, RegisterRequestDtoValidator>();

            // Password Hasher Injection
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        }
    }
}
