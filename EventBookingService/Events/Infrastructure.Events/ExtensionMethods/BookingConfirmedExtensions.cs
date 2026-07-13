using Application.Events.DTO.Requests;

using Contracts.Messages;

namespace Infrastructure.Events.ExtensionMethods;

internal static class BookingConfirmedExtensions
{
    internal static BookingConfirmedRequest ToBookingConfirmedRequest(this BookingConfirmed bookingConfirmed) => new
    (
        bookingConfirmed.BookingId,
        bookingConfirmed.EventId,
        bookingConfirmed.UserId,
        bookingConfirmed.Seats,
        bookingConfirmed.Approved
    );
}
