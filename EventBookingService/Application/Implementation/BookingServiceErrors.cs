namespace Application.Implementation;

public static class BookingServiceErrors
{
    public const string InvalidMaxCount = "Количество броней для обработки не должны быть меньше 1";
    public const string NoAvailableSeats = "На данное событие больше нет мест";
    public static string EventNotFound(Guid id) => $"Событие с идентификатором {id} не найдено";
    public static string BookingNotFound(Guid id) => $"Бронирование с идентификатором {id} не найдено";
    public static string BookingAccessDenied(Guid id) => $"Бронирование с идентификатором {id} принадлежит другому пользователю";
    public static string BookingAlreadyCancelled(Guid id) => $"Бронирование с идентификатором {id} нельзя повторно отменить";
    public static string ExceedBookingLimit(int maxCount) => $"У этого пользователя уже достигнут лимит по количеству активных бронирований ({maxCount})";
}
