using Domain.Bookings;

namespace Application.DTO.Bookings.Response;

/// <summary>
/// Ответ на запрос получения брони
/// </summary>
public class BaseBookingResponse
{
    /// <summary>
    /// Идентификатор брони
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

    /// <summary>
    /// Дата и время создания бронирования
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Дата и время обработки бронирования с установлением решения (подтвердить/отклонить)
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; set; }

    public static BaseBookingResponse FromBooking(Booking entity) => new()
    {
        Id = entity.Id,
        EventId = entity.EventId,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt,
        ProcessedAt = entity.ProcessedAt
    };
}
