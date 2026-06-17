namespace Application.Infrastructure;

/// <summary>
/// Менеджер хэширования и проверки паролей
/// </summary>
public interface IPasswordManager
{
    /// <summary>
    /// Создание хэша пароля
    /// </summary>
    /// <param name="pass">Пароль</param>
    /// <returns>Строка с хэшем в необходимом формате</returns>
    string HashPassword(string pass);

    /// <summary>
    /// Проверка пароля на совпадение с хэшем
    /// </summary>
    /// <param name="pass">Пароль</param>
    /// <param name="storedHash">Хэш для сравнения</param>
    /// <returns>true если хэш пароля совпадает с переданным</returns>
    bool VerifyPassword(string pass, string storedHash);
}
