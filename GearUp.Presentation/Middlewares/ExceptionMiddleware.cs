namespace GearUp.Presentation.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger    )
        {
            _next = next;
            _logger = logger;
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
                var response = new { IsSuccess = false, Message = "An internal server error occurred. " + ex };
                _logger.LogError(ex, "An unhandled exception occurred while processing the request: {Message}", ex.Message);
                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}
