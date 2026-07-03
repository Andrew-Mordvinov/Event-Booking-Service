namespace Application.Bookings.Infrastructure
{
    /// <summary>
    /// Интерфейс, представляющий собой методы для работы с пользователем, от имени которого идет взаимодействие с сервисом
    /// </summary>
    public interface IUserContext
    {
        /// <summary>
        /// Проверка, что текущий пользователь админ
        /// </summary>
        /// <param name="token">Токен отмены асинхронной операции</param>
        /// <returns>Истина, если пользователь админ</returns>
        Task<bool> IsAdmin(CancellationToken token = default);

        /// <summary>
        /// Идентификатор пользователя
        /// </summary>
        Guid UserId { get; }
    }
}
