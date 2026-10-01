using System.Security.Authentication;
using System.Text.Json;

namespace OpsFlow.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception) when (!context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                "An unhandled exception occurred while processing {RequestMethod} {RequestPath}",
                context.Request.Method,
                context.Request.Path);

            var (statusCode, message) = MapException(exception);
            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = new
            {
                statusCode,
                message,
                details = _environment.IsDevelopment() ? exception.StackTrace : null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }

    private static (int StatusCode, string Message) MapException(Exception exception)
    {
        return exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            AuthenticationException => (StatusCodes.Status401Unauthorized, exception.Message),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, exception.Message),
            InvalidOperationException => (StatusCodes.Status400BadRequest, exception.Message),
            ArgumentException => (StatusCodes.Status400BadRequest, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };
    }
}
