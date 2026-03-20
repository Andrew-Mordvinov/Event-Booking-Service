
using Microsoft.AspNetCore.Mvc;

namespace EventBookingService.Middleware;

/// <summary>
/// Глобальный перехватчик исключений с логгированием и формированием
/// ответа в единообразной структуре
/// </summary>
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    #region Private fields

    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

    #endregion

    #region Public methods

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

    #endregion

    #region Private methods

    private async Task Handle(HttpContext context, Exception exception)
    {
        _logger.LogError(
            exception,
            "Возникло необработанное исключение. Method={Method}, Path={Path}, TraceId={TraceId}",
            context.Request.Method,
            context.Request.Path,
            context.TraceIdentifier);

        if (context.Response.HasStarted)
        {
            return;
        }

        var code = GetStatusCode(exception);

        context.Response.StatusCode = code;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "Unexpected error",
            Status = code,
            Detail = exception.Message,
            Instance = context.Request.Path,
            Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
        });
    }

    #endregion

    #region Private static methods

    // Пока все ошибки обрабатываются без исключений и любое исключение - внутренняя ошибка сервера
    private static int GetStatusCode(Exception exception) => StatusCodes.Status500InternalServerError;

    #endregion
}
