namespace EventBookingService.Common.Validations;

/// <summary>
/// Класс, объект которого содержит искомый результат (объект типа) и сообщения
/// об ошибках при его получении
/// </summary>
/// <typeparam name="T">Тип содержимого</typeparam>
public class ValidationResult<T>
{
    protected List<string> _errors = new List<string>();

    public T? Value { get; set; }

    public IReadOnlyCollection<string> Errors { get => _errors.AsReadOnly(); }

    public  ValidationResult()
    {
        
    }

    public ValidationResult(T? val)
    {
        Value = val;
    }

    public ValidationResult(T? val, IEnumerable<string> errors)
        :this(val)
    {
        AddErrors(errors);     
    }

    public ValidationResult(T? val, string error)
        :this(val)
    {
        AddError(error);
    }

    public void AddError(string error)
    {
        _errors.Add(error);
    }

    public void AddErrors(IEnumerable<string> errors)
    {
        _errors.AddRange(errors);
    }

    public bool IsSuccessful => !_errors.Any();
}
