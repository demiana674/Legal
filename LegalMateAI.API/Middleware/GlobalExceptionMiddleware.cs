using System.Net;
using System.Text.Json;
using LegalMateAI.DTOs;

namespace LegalMateAI.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var (statusCode, message) = exception switch
            {
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, exception.Message),
                KeyNotFoundException => (HttpStatusCode.NotFound, "العنصر غير موجود"),
                ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
                TimeoutException => (HttpStatusCode.RequestTimeout, "استغرق الطلب وقتاً أطول من المتوقع"),
                _ => (HttpStatusCode.InternalServerError, "حدث خطأ داخلي في الخادم")
            };

            var response = new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };

            context.Response.StatusCode = (int)statusCode;

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await context.Response.WriteAsync(json);
        }
    }
}