using System.Net;
using System.Text.Json;

namespace ECommerce.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(
                ex,
                "Unhandled exception for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await HandleExceptionAsync(
                context,
                ex);
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        var statusCode = exception switch
        {
            InvalidOperationException =>
                (int)HttpStatusCode.BadRequest,

            KeyNotFoundException =>
                (int)HttpStatusCode.NotFound,

            UnauthorizedAccessException =>
                (int)HttpStatusCode.Unauthorized,

            _ =>
                (int)HttpStatusCode.InternalServerError
        };

        var message = exception switch
        {
            InvalidOperationException =>
                exception.Message,

            KeyNotFoundException =>
                exception.Message,

            UnauthorizedAccessException =>
                exception.Message,

            _ =>
                "An unexpected error occurred."
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType =
            "application/problem+json";

        var problemDetails = new
        {
            type =
                $"https://httpstatuses.com/{statusCode}",

            title = GetTitle(statusCode),

            status = statusCode,

            detail = message,

            instance = context.Request.Path.ToString(),

            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails));
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            404 => "Not Found",
            500 => "Internal Server Error",
            _ => "Error"
        };
    }
}