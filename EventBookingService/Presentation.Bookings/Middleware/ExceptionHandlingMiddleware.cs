
using Presentation.Bookings.Exceptions;

namespace Presentation.Bookings.Middleware;

/// <summary>
/// Глобальный перехватчик исключений с логгированием и формированием
/// ответа в единообразной структуре
/// </summary>
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    ExceptionHandlerRegistry handlerRegistry)
{
    #region Private fields

    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;
    private readonly ExceptionHandlerRegistry _handlerRegistry = handlerRegistry;

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

        var code = _handlerRegistry.GetStatusCode(exception);

        context.Response.StatusCode = code;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(_handlerRegistry.GetProblemDetails(context, exception));
    }

    #endregion
}
