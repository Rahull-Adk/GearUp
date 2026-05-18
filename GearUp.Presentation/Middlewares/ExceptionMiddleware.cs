using System.Text.Json;
using GearUp.Domain.Exceptions;
using GearUp.Presentation.DTOs;

namespace GearUp.Presentation.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                _logger.LogInformation("Processing request: {Method} {Path}", context.Request.Method, context.Request.Path);
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var response = new ApiResponse<object>
            {
                IsSuccess = false,
                Status = 500,
                Message = "An internal server error occurred."
            };

            if (exception is BaseException baseException)
            {
                response.Status = baseException.StatusCode;
                response.Message = baseException.Message;
            }
            else if (exception is FluentValidation.ValidationException fluentException)
            {
                response.Status = 422;
                response.Message = string.Join("; ", fluentException.Errors.Select(e => e.ErrorMessage));
            }
            else
            {
                _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
            }

            if (_environment.IsDevelopment() && response.Status == 500)
            {
                response.Message = exception.Message;
                // Optionally add stack trace or details for 500 errors in dev
            }

            context.Response.StatusCode = response.Status;

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}
