using Microsoft.AspNetCore.Mvc;

namespace Presentation.Events.Exceptions;

/// <summary>
/// Реестр исключений, расширяющий доменные типы для возврата ответов в web
/// </summary>
public class ExceptionHandlerRegistry
{
    private readonly Dictionary<Type, ExceptionHandler> _handlers = new();

    /// <summary>
    /// Регистрация типа исключения с кастомными обработчиками
    /// </summary>
    /// <param name="statusCode">Статус-код в ответе http</param>
    /// <param name="problemDetailsFactory">Фабрика для исключений, создающая problem details</param>
    public ExceptionHandlerRegistry Register<TException>(
        int statusCode,
        Func<HttpContext, TException, ProblemDetails> problemDetailsFactory)
        where TException : Exception
    {
        _handlers[typeof(TException)] = new ExceptionHandler
        {
            StatusCode = statusCode,
            ProblemDetailsFactory = (context, ex) =>
            {
                var problemDetails = problemDetailsFactory(context, (TException) ex);
                problemDetails.Status = statusCode;

                return problemDetails;
            }
        };

        return this;
    }

    /// <summary>
    /// Получить статус-код
    /// </summary>
    /// <param name="exception">Исключение, для которого нужно получить статус-код</param>
    /// <returns>Целочисленный код</returns>
    public int GetStatusCode(Exception exception)
    {
        return _handlers.TryGetValue(exception.GetType(), out var handler)
            ? handler.StatusCode
            : StatusCodes.Status500InternalServerError;
    }

    /// <summary>
    /// Получить ProblemDetails для исключения и текущего контекста
    /// </summary>
    /// <param name="context">Контекст запроса</param>
    /// <param name="exception">Возникшее исключение</param>
    /// <returns>ProblemDetails</returns>
    public ProblemDetails GetProblemDetails(HttpContext context, Exception exception)
    {
        if (_handlers.TryGetValue(exception.GetType(), out var handler))
        {
            return handler.ProblemDetailsFactory(context, exception);
        }
        return GetBaseProblemDetails(context, exception);
    }

    /// <summary>
    /// Базовый ProblemDetails, если для типа исключения не зарегистрирован никакой специальный обработчик
    /// </summary>
    /// <param name="context">Контекст запроса</param>
    /// <param name="exception">Возникшее исключение</param>
    /// <returns>ProblemDetails</returns>
    private static ProblemDetails GetBaseProblemDetails(HttpContext context, Exception exception) =>
        new ProblemDetails
        {
            Title = "Unexpected error",
            Instance = context.Request.Path,
            Detail = exception.Message,
            Status = StatusCodes.Status500InternalServerError,
            Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
        };

    /// <summary>
    /// Тип обработчика исключения, содержащий код статуса и генератор ProblemDetails
    /// </summary>
    private record ExceptionHandler
    {
        public int StatusCode { get; init; }
        public required Func<HttpContext, Exception, ProblemDetails> ProblemDetailsFactory { get; init; }
    }
}
