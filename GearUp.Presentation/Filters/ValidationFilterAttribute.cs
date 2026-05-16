using GearUp.Presentation.DTOs;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GearUp.Presentation.Filters
{
    public class ValidationFilterAttribute : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument == null) continue;

                var argumentType = argument.GetType();
                var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
                var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

                if (validator != null)
                {
                    var validationContext = new ValidationContext<object>(argument);
                    var validationResult = await validator.ValidateAsync(validationContext);

                    if (!validationResult.IsValid)
                    {
                        var errors = validationResult.Errors
                            .Select(e => e.ErrorMessage)
                            .ToList();

                        var errorMessage = string.Join(" ", errors);
                        
                        context.Result = new BadRequestObjectResult(ApiResponse<object>.Failure(errorMessage, 400));
                        return;
                    }
                }
            }

            await next();
        }
    }
}
