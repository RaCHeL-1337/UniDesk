using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using UniDesk.Web.Exceptions;

namespace UniDesk.Web.Middleware;

public class GlobalExceptionLoggingMiddleware
{
    private readonly ILogger<GlobalExceptionLoggingMiddleware> _logger;
    private readonly RequestDelegate _next;

    public GlobalExceptionLoggingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionLoggingMiddleware> logger)
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
        catch (Exception ex) when (ex is EntityNotFoundException or BadHttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception while processing {RequestMethod} {RequestPath}",
                context.Request.Method,
                context.Request.Path.Value);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal server error",
                Detail = "An unexpected error occurred.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
