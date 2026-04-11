namespace Validation;

/// <summary>
/// Хэлпер для более явного указания намерений в коде
/// + чтобы не писать в конструкторе в угловых скобках каждый раз тип
/// </summary>
public static class ResultCreator
{
    public static ValidationResult<T?> Success<T>(T? val) => new(val);

    public static ValidationResult<T?> Fail<T>(T? val, IEnumerable<string> errors) => new(val, errors);

    public static ValidationResult<T?> Fail<T>(T? val, string error) => new(val, error);

    public static ValidationResult Success() => new();

    public static ValidationResult Fail(IEnumerable<string> errors) => new(errors);

    public static ValidationResult Fail(string error) => new(error);

    public static ValidationResult<T?> ToGeneric<T>(this ValidationResult result, T? val)
    {
        return result.IsSuccessful ?
            Success(val)
            : Fail(val, result.Errors);
    }
}
