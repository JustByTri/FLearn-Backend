namespace Presentation.Middlewares
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);

                if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
                {
                    await WriteJsonResponse(context, StatusCodes.Status401Unauthorized,
                        "Authentication required. Please login first.");
                }
                else if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
                {
                    await WriteJsonResponse(context, StatusCodes.Status403Forbidden,
                        "You do not have permission to access this resource.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception caught in middleware.");

                await WriteJsonResponse(context, StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred. Please try again later.");
            }
        }

        private static async Task WriteJsonResponse(HttpContext context, int statusCode, string message)
        {
            if (context.Response.HasStarted) return;

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                code = statusCode,
                message
            };

            var json = System.Text.Json.JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);
        }
    }

    public static class ErrorHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseErrorHandlingMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ErrorHandlingMiddleware>();
        }
    }
}
