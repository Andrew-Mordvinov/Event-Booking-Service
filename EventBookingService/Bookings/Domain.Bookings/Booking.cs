using Shared.Entities;

namespace Domain.Bookings;

/// <summary>
/// Модель бронирования события
/// </summary>
public class Booking : IHasId, ICopyable<Booking>
{
    public Guid Id { get; protected set; }

    public Guid EventId { get; protected set; }

    public Guid UserId { get; protected set; }

    public BookingStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; protected set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    protected Booking()
    {

    }

    public Booking(
        Guid id,
        Guid eventId,
        Guid userId,
        BookingStatus status,
        DateTimeOffset created,
        DateTimeOffset? processed = null)
    {
        Id = id;
        EventId = eventId;
        UserId = userId;
        Status = status;
        CreatedAt = created;
        ProcessedAt = processed;
    }

    public Booking Copy() => new(Id, EventId, UserId, Status, CreatedAt, ProcessedAt);

    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = BookingStatus.Cancelled;
    }
}
