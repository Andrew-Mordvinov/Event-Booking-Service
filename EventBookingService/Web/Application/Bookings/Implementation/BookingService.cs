using EventBookingService.Application.Events;
using EventBookingService.Common.Storage;
using EventBookingService.Common.Validations.Results;
using EventBookingService.Models.Bookings;

namespace EventBookingService.Application.Bookings.Implementation;

public class BookingService(
    [FromKeyedServices("Mem")] IStorage<Booking> _storageBooking,
    IEventService _eventService) : IBookingService
{
    public async Task<ValidationResult<Booking?>> CreateBookingAsync(
        Guid eventId,
        CancellationToken token = default)
    {
        var eventResult = await _eventService.GetEventByIdAsync(eventId, token);

        if (!eventResult.IsSuccessful)
        {
            return ResultCreator.Fail<Booking?>(null, eventResult.Errors);
        }

        if (eventResult.Value is null)
        {
            return ResultCreator.Success<Booking?>(null);
        }

        var booking = new Booking(Guid.NewGuid(), eventId, BookingStatus.Pending, DateTime.UtcNow);

        var result = await _storageBooking.AddAsync(booking, token);

        return result.IsSuccessful ?
            result.ToGeneric(booking)
            : result.ToGeneric<Booking>(null);
    }

    public Task<ValidationResult<Booking?>> GetBookingByIdAsync(
        Guid bookingId,
        CancellationToken token = default) =>
        _storageBooking.GetByIdAsync(bookingId, token);
}
