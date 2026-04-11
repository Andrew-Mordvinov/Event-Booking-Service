namespace Validation;

/// <summary>
/// Класс, объект которого содержит только ошибки, возникшие при выполнении некоторой операции,
/// которая не возвращает результат
/// </summary>
public class ValidationResult
{
    #region Protected fields

    protected List<string> _errors = [];

    #endregion

    #region Properties

    public IReadOnlyCollection<string> Errors => _errors.AsReadOnly();

    public bool IsSuccessful => _errors.Count == 0;

    #endregion

    #region Constructors

    public ValidationResult()
    {

    }

    public ValidationResult(IEnumerable<string> errors) => AddErrors(errors);

    public ValidationResult(string error) => AddError(error);

    #endregion

    #region Public methods

    public void AddError(string error) => _errors.Add(error);

    public void AddErrors(IEnumerable<string> errors) => _errors.AddRange(errors);

    #endregion
}

/// <summary>
/// Класс, объект которого содержит искомый результат (объект типа) и сообщения
/// об ошибках при его получении
/// </summary>
/// <typeparam name="T">Тип содержимого</typeparam>
public class ValidationResult<T> : ValidationResult
{
    public T? Value { get; set; }

    public ValidationResult(T? val) => Value = val;

    public ValidationResult(T? val, IEnumerable<string> errors)
        :this(val) => AddErrors(errors);

    public ValidationResult(T? val, string error)
        :this(val) => AddError(error);
}
