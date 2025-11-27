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
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                
                _logger.LogError(ex, "An unhandled exception occurred while processing the request: {Message}", ex.Message);

                // Only expose detailed exception info in development
                object response;
                if (_environment.IsDevelopment())
                {
                    response = new { IsSuccess = false, Message = "An internal server error occurred.", Details = ex.ToString() };
                }
                else
                {
                    response = new { IsSuccess = false, Message = "An internal server error occurred. Please try again later." };
                }

                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}
