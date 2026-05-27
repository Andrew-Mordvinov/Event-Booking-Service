namespace Entities.Bookings;

/// <summary>
/// Статусы бронирования события
/// </summary>
public enum BookingStatus
{
    /// <summary>
    /// Ожидает обработку
    /// </summary>
    Pending,

    /// <summary>
    /// Бронирование подтверждено
    /// </summary>
    Confirmed,

    /// <summary>
    /// Бронирование отклонено
    /// </summary>
    Rejected
}
