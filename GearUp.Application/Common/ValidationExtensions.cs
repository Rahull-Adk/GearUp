using FluentValidation;

namespace GearUp.Application.Common
{
    public static class ValidationExtensions
    {
        public static void EnsureValid<T>(this IValidator<T> validator, T instance)
        {
            var result = validator.Validate(instance);
            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        public static async Task EnsureValidAsync<T>(this IValidator<T> validator, T instance, CancellationToken cancellationToken = default)
        {
            var result = await validator.ValidateAsync(instance, cancellationToken);
            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }
    }
}
