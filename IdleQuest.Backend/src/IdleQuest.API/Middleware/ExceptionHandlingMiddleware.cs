using System.Net;
using System.Text.Json;
using IdleQuest.Domain;

namespace IdleQuest.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (DomainException ex)
        {
            await Write(ctx, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            await Write(ctx, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    private static Task Write(HttpContext ctx, HttpStatusCode code, string message)
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode = (int)code;
        return ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
    }
}
