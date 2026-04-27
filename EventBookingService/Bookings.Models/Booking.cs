using Shared.Interfaces;

namespace Bookings.Models;

/// <summary>
/// Модель бронирования события
/// </summary>
public class Booking(Guid id, Guid eventId, BookingStatus status, DateTime created, DateTime? processed = null) 
    : IHasId, ICopyable<Booking>
{
    public Guid Id { get; } = id;

    public Guid EventId { get; protected set; } = eventId;

    public BookingStatus Status { get; set; } = status;

    public DateTime CreatedAt { get; protected set; } = created;

    public DateTime? ProcessedAt { get; set; } = processed;

    public Booking Copy() => new(Id, EventId, Status, CreatedAt, ProcessedAt);

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
}
