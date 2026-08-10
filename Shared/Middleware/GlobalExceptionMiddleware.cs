using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Shared.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception has occurred while executing the request.");
            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        // Default to Internal Server Error
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        var message = "An unexpected error occurred while processing your request.";

        // Handle specific known exceptions
        if (exception is KeyNotFoundException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            message = exception.Message;
        }
        else if (exception is UnauthorizedAccessException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            message = exception.Message;
        }
        else if (exception is InvalidOperationException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            message = exception.Message;
        }

        var response = new { message };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
