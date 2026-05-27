namespace Application.Implementation;

public static class BookingServiceErrors
{
    public const string InvalidMaxCount = "Количество броней для обработки не должны быть меньше 1";
    public const string NoAvailableSeats = "На данное событие больше нет мест";
    public static string EventNotFound(Guid id) => $"Событие с идентификатором {id} не найдено";
}
