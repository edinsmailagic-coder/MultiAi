using System.Net;
using System.Security.Claims;
using System.Text.Json;
using MultiAI.Services.Interface;

namespace MultiAI.Middleware
{
    public class ExceptionLoggingMiddleware
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly RequestDelegate next;
        private readonly ILogger<ExceptionLoggingMiddleware> logger;

        public ExceptionLoggingMiddleware(RequestDelegate next, ILogger<ExceptionLoggingMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IErrorLogService errorLog)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

                try
                {
                    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    await errorLog.LogAsync(
                        userId,
                        context.Request.Path,
                        context.Request.Method,
                        ex,
                        statusCode: (int)HttpStatusCode.InternalServerError,
                        source: "middleware");
                }
                catch (Exception logEx)
                {
                    logger.LogError(logEx, "Failed to write ErrorLog entry");
                }

                if (context.Response.HasStarted)
                    throw;

                context.Response.Clear();
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var payload = new
                {
                    title = "An unexpected error occurred.",
                    status = 500,
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
            }
        }
    }
}
