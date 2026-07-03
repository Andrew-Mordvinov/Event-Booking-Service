using Application.Events.DTO.Requests;
using Infrastructure.Events.Ef.Models;

namespace Infrastructure.Events.ExtensionMethods;

internal static class BookingConfirmedRequestExtensions
{
    internal static BookingConfirmedInboxItem ToBookingConfirmedInboxItem(this BookingConfirmedRequest request) => new
    (
        request.BookingId,
        request.EventId,
        request.UserId,
        request.Seats,
        request.Approved
    );
}
