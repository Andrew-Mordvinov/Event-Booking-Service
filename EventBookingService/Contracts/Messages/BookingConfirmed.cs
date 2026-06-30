namespace Contracts.Messages;

/// <summary>
/// Событие "Бронирование подтверждено"
/// </summary>
/// <param name="BookingId">Идентификатор бронировани</param>
/// <param name="EventId">Идентификатор события</param>
/// <param name="UserId">Идентификатор пользователя</param>
/// <param name="Seats">Количество мест</param>
/// <param name="Approved">Дата и время подтверждения</param>
public record BookingConfirmed(Guid BookingId, Guid EventId, Guid UserId, int Seats, DateTimeOffset Approved);
