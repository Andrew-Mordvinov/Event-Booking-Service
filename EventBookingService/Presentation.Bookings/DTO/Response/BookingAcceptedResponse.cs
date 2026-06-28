using Domain.Bookings;

namespace Presentation.Bookings.DTO.Response;

/// <summary>
/// Ответ на запрос о бронировании события
/// </summary>
public class BookingAcceptedResponse
{
    /// <summary>
    /// Идентификатор созданного бронирования
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Идентификатор события, по которому создана бронь
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// Статус бронирования
    /// </summary>
    public BookingStatus Status { get; init; }

    public static BookingAcceptedResponse FromBooking(Booking entity) => new()
    {
        Id = entity.Id,
        EventId = entity.EventId,
        Status = entity.Status,
    };
}
