namespace Infrastructure.Events.Ef.Models;

/// <summary>
/// Элемент хранилища входящих сообщений для обработки
/// Пока по сути повторяет общий контракт
/// </summary>
public record BookingConfirmedInboxItem(Guid BookingId, Guid EventId, Guid UserId, int Seats, DateTimeOffset Approved);
