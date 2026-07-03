namespace Application.Events.DTO.Requests;

/// <summary>
/// Запрос "Бронирование подтверждено" (пока что просто копия сообщения)
/// </summary>
/// <param name="BookingId">Идентификатор бронировани</param>
/// <param name="EventId">Идентификатор события</param>
/// <param name="UserId">Идентификатор пользователя</param>
/// <param name="Seats">Количество мест</param>
/// <param name="Approved">Дата и время подтверждения</param>
public record BookingConfirmedRequest(Guid BookingId, Guid EventId, Guid UserId, int Seats, DateTimeOffset Approved);
