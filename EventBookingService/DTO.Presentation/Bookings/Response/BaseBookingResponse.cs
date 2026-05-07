using Entities.Bookings;

namespace DTO.Presentation.Bookings.Response;

/// <summary>
/// Ответ на запрос о бронировании события. Представляет собой
/// проекцию <see cref="Booking"/> с теми частью свойств
/// </summary>
public class BookingAcceptedResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public BookingStatus Status { get; set; }

    public static BookingAcceptedResponse FromBooking(Booking entity) => new()
    {
        Id = entity.Id,
        EventId = entity.EventId,
        Status = entity.Status,
    };
}
