namespace EventBookingService.Models.Bookings.Response;

/// <summary>
/// Базовый ответ на запрос по сущности <see cref="Booking"/>. Представляет собой
/// проекцию <see cref="Booking"/> с теми свойствами, которые должны быть переданы в
/// качестве ответа на запрос
/// </summary>
public class BaseBookingResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public static BaseBookingResponse FromBooking(Booking entity) => new()
    {
        Id = entity.Id,
        EventId = entity.EventId,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt,
        ProcessedAt = entity.ProcessedAt
    };
}
