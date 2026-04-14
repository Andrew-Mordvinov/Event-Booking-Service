using Microsoft.AspNetCore.Mvc;

using Validation;

namespace Web.Common.Validations;

/// <summary>
/// Конвертер ошибок из <see cref="ValidationResult{T}"/> в <see cref="ProblemDetails"/> 
/// для стандартизованного возврата результата об ошибках
/// </summary>
public static class ErrorToProblemDetailsConverter
{
    /// <summary>
    /// Конвертация ошибок в результате валидации, если они есть, в список с элементами <see cref="ProblemDetails"/>
    /// для стандартизованного вывода ошибок
    /// </summary>
    /// <param name="result">Результат валидации</param>
    /// <returns>Массив <see cref="ProblemDetails"/></returns>
    public static IEnumerable<ProblemDetails> ToProblemDetails<T>(
        this ValidationResult<T> result,
        HttpContext context) =>
        result.Errors.Select(e => new ProblemDetails
        {
            Instance = context.Request.Path,
            Detail = e.Text,
            Status = CategoryToCode(e.Category),
            Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
        });

    private static int? CategoryToCode(ItemCategory category) => category switch
    {
        ItemCategory.ValidationError => StatusCodes.Status400BadRequest,
        ItemCategory.ConflictError => StatusCodes.Status409Conflict,
        _ => null
    };
}
