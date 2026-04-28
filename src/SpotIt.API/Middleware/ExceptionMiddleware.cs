using FluentValidation;
using SpotIt.Application.Exceptions;
using System.Text.Json;

namespace SpotIt.API.Middleware;

public class ExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            context.Response.ContentType = "application/json";

            if (ex is NotFoundException)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            else if (ex is ValidationException)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { errors = ((ValidationException)ex).Errors.Select(e => e.ErrorMessage)}));
            }
            else
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message, inner = ex.InnerException?.Message, inner2 = ex.InnerException?.InnerException?.Message }));
            }


        }
    }
}
