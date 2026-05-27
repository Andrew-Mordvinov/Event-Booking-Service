using Domain.Exceptions;

using Microsoft.AspNetCore.Mvc;

namespace Presentation.Middleware;

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

        await context.Response.WriteAsJsonAsync(GetProblemDetails(context, exception));
    }

    #endregion

    #region Private static methods

    private static int GetStatusCode(Exception exception) =>
        (exception) switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            ConflictException => StatusCodes.Status409Conflict,
            NotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError,
        };

    private static IEnumerable<ProblemDetails> GetProblemDetails(HttpContext context, Exception exception) =>
        (exception) switch
        {
            ValidationException ex => GetValidationProblemDetails(context, ex),
            ConflictException ex => GetConflictProblemDetails(context, ex),
            NotFoundException ex => GetNotFoundProblemDetails(context, ex),
            _ => GetBaseProblemDetails(context, exception),
        };

    private static IEnumerable<ProblemDetails> GetValidationProblemDetails(HttpContext context, ValidationException exception) =>
        exception.Errors.Select(e => new ProblemDetails
        {
            Title = "Validation error",
            Instance = context.Request.Path,
            Detail = e,
            Status = StatusCodes.Status400BadRequest,
            Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
        });

    private static IEnumerable<ProblemDetails> GetNotFoundProblemDetails(HttpContext context, NotFoundException exception) =>
        [
            new ProblemDetails
            {
                Title = "Not found",
                Instance = context.Request.Path,
                Detail = exception.Message,
                Status = StatusCodes.Status404NotFound,
                Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
            }
        ];

    private static IEnumerable<ProblemDetails> GetConflictProblemDetails(HttpContext context, ConflictException exception) =>
        [
            new ProblemDetails
            {
                Title = "Conflict found",
                Instance = context.Request.Path,
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict,
                Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
            }
        ];

    private static IEnumerable<ProblemDetails> GetBaseProblemDetails(HttpContext context, Exception exception) =>
        [
            new ProblemDetails
            {
                Title = "Unexpected error",
                Instance = context.Request.Path,
                Detail = exception.Message,
                Status = StatusCodes.Status500InternalServerError,
                Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
            }
        ];
    #endregion
}
