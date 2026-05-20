using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using UniDesk.Web.Exceptions;

namespace UniDesk.Web.Middleware;

public class EntityNotFoundExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public EntityNotFoundExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (EntityNotFoundException ex)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Entity not found",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5"
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
        catch (BadHttpRequestException ex)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
