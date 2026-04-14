namespace Validation;

/// <summary>
/// Класс, объект которого содержит только ошибки, возникшие при выполнении некоторой операции,
/// которая не возвращает результат
/// </summary>
public class ValidationResult
{
    #region Protected fields

    protected List<ValidationItem> _items = [];

    #endregion

    #region Properties

    public IReadOnlyCollection<ValidationItem> Errors => _items.AsReadOnly();

    public bool IsSuccessful => !_items.Any(t => t.IsError);

    #endregion

    #region Constructors

    public ValidationResult()
    {

    }

    public ValidationResult(IEnumerable<string> errors) => AddErrors(errors);

    public ValidationResult(string error) => AddError(error);

    public ValidationResult(IEnumerable<ValidationItem> errors) => AddItems(errors);

    public ValidationResult(ValidationItem error) => AddItem(error);

    #endregion

    #region Public methods

    public void AddError(string error) => _items.Add(new ValidationItem(error));

    public void AddItem(ValidationItem error) => _items.Add(error);

    public void AddItems(IEnumerable<ValidationItem> errors) => _items.AddRange(errors);

    public void AddErrors(IEnumerable<string> errors) => _items.AddRange(errors.Select(t => new ValidationItem(t)));

    public bool HasCategory(ItemCategory category) => _items.Any(t => t.Category == category);

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

    public ValidationResult(T? val, IEnumerable<ValidationItem> errors)
        :this(val) => AddItems(errors);

    public ValidationResult(T? val, ValidationItem error)
        :this(val) => AddItem(error);
}
