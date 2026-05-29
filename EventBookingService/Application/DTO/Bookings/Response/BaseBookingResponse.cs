using Domain.Bookings;

namespace Application.DTO.Bookings.Response;

/// <summary>
/// Ответ на запрос о бронировании события
/// </summary>
public class BookingAcceptedResponse
{
    /// <summary>
    /// Идентификатор созданного бронирования
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор события, по которому создана бронь
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Статус бронирования
    /// </summary>
    public BookingStatus Status { get; set; }

    public static BookingAcceptedResponse FromBooking(Booking entity) => new()
    {
        Id = entity.Id,
        EventId = entity.EventId,
        Status = entity.Status,
    };
}
