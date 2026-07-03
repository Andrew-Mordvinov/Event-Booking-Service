namespace Application.Users.Validations;

/// <summary>
/// Валидатор запросов на регистрацию пользователя
/// </summary>
public interface IValidator<T> where T : class
{
    /// <summary>
    /// Валидация аргумента и возврат массива ошибок
    /// </summary>
    /// <param name="item">Объект для проверки</param>
    /// <returns>Список ошибок (может быть пуст, если все хорошо)</returns>
    public IEnumerable<string> Validate(T item);
}
