using Domain.Bookings;

using Infrastructure.Bookings.Ef.Models;

namespace Infrastructure.Bookings.ExtensionMethods;

internal static class BookingExtensions
{
    internal static BookingConfirmedOutboxItem ToBookingConfirmedOutboxItem(this Booking booking) => new
    (
        booking.Id,
        booking.EventId,
        booking.UserId,
        1,
        booking.ProcessedAt ?? DateTimeOffset.Now
    );
}
