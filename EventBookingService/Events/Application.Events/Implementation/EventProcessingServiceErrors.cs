namespace Application.Events.Implementation;

public static class EventProcessingServiceErrors
{
    public static string EventNotFound(Guid id) => $"Событие с идентификатором {id} не найдено";

    public static string EventHasNoSeats(Guid id, int seats) => $"У события с идентификатором {id} нет мест ({seats}) для бронирования";
}
