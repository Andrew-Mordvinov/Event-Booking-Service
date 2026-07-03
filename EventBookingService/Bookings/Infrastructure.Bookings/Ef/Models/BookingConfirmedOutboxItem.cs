namespace Infrastructure.Bookings.Ef.Models;

/// <summary>
/// Элемент хранилища исходящих сообщений для обработки брокером
/// Пока по сути повторяет общий контракт
/// </summary>
public record BookingConfirmedOutboxItem(Guid BookingId, Guid EventId, Guid UserId, int Seats, DateTimeOffset Approved);
