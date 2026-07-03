using Domain.Bookings;

namespace Presentation.Bookings.DTO.Response;

/// <summary>
/// Ответ на запрос получения брони
/// </summary>
public class BaseBookingResponse
{
    /// <summary>
    /// Идентификатор брони
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Идентификатор события, по которому создана бронь
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// Идентификатор пользователя, для которого создана бронь
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Статус бронирования
    /// </summary>
    public BookingStatus Status { get; init; }

    /// <summary>
    /// Дата и время создания бронирования
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Дата и время обработки бронирования с установлением решения (подтвердить/отклонить)
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; init; }

    public static BaseBookingResponse FromBooking(Booking entity) => new()
    {
        Id = entity.Id,
        EventId = entity.EventId,
        UserId = entity.EventId,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt,
        ProcessedAt = entity.ProcessedAt
    };
}
