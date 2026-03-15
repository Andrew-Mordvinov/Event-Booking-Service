
using Microsoft.AspNetCore.Mvc;

namespace EventBookingService.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
            await Handle(context, ex);
        }
    }

    private async Task Handle(HttpContext context, Exception exception)
    {
        _logger.LogError(
            exception,
            "Возникло необработанное исключение. Method={Method}, Path={Path}, RequestId={RequestId}",
            context.Request.Method,
            context.Request.Path,
            context.Request.Headers["x-request-id"]);

        if (context.Response.HasStarted)
        {
            return;
        }

        var code = GetStatusCode(exception);

        context.Response.StatusCode = code;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = code, Detail = exception.Message });
    }

    // Пока все ошибки обрабатываются без исключений и любое исключение - внутренняя ошибка сервера
    private int GetStatusCode(Exception exception) => StatusCodes.Status500InternalServerError;
}
