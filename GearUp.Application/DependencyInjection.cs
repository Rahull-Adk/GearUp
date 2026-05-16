using FluentValidation;
using GearUp.Application.Validators;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace GearUp.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            return services;
        }
    }
}
