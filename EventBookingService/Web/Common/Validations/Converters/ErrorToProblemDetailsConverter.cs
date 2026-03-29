using EventBookingService.Common.Validations.Results;

using Microsoft.AspNetCore.Mvc;

namespace EventBookingService.Common.Validations.Converters;

/// <summary>
/// Конвертер ошибок из <see cref="ValidationResult{T}"/> в <see cref="ProblemDetails"/> 
/// для стандартизованного возврата результата об ошибках
/// </summary>
public static class ErrorToProblemDetailsConverter
{
    // В будущем возможно error будет не просто строкой и код будет маппиться внутри по своим признакам, но пока
    // для стандарта вывода ошибок хватает строки

    /// <summary>
    /// Конвертация ошибок в результате валидации, если они есть, в список с элементами <see cref="ProblemDetails"/>
    /// для стандартизованного вывода ошибок
    /// </summary>
    /// <param name="result">Результат валидации</param>
    /// <param name="code">Код ошибки, который присваивается каждому элементу полученной коллекции по умолчанию. 
    /// Если не указано явно, то 400</param>
    /// <returns>Массив <see cref="ProblemDetails"/></returns>
    public static IEnumerable<ProblemDetails> ToProblemDetails<T>(
        this ValidationResult<T> result,
        HttpContext context,
        int code = StatusCodes.Status400BadRequest) =>
        result.Errors.Select(e => new ProblemDetails
        {
            Instance = context.Request.Path,
            Detail = e,
            Status = code,
            Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
        });
}
