using EventBookingService.Application.Events;
using EventBookingService.Common.Storage;
using EventBookingService.Common.Validations.Results;
using EventBookingService.Models.Bookings;

namespace EventBookingService.Application.Bookings.Implementation;

public class BookingService(
    [FromKeyedServices("Mem")] IStorage<Booking> _storageBooking,
    IEventService _eventService,
    ILogger<BookingService> _logger) : IBookingService
{
    public Task<ValidationResult<Booking?>> GetBookingByIdAsync(
        Guid bookingId,
        CancellationToken token = default) =>
        _storageBooking.GetByIdAsync(bookingId, token);

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

    public async Task<ValidationResult> ProcessPendingBookingsAsync(int maxCount = 100, CancellationToken token = default)
    {
        if (maxCount < 1)
        {
            return ResultCreator.Fail(BookingServiceErrors.InvalidMaxCount);
        }

        var pageResult = await _storageBooking.GetPageAsync(
                b => b.Status == BookingStatus.Pending,
                1,
                maxCount,
                token);

        if (!pageResult.IsSuccessful)
        {
            _logger.LogError("При получении броней возникли ошибки: {@Errors}", pageResult.Errors);
            return pageResult;
        }

        token.ThrowIfCancellationRequested();

        foreach (var book in pageResult.Value?.Items ?? Enumerable.Empty<Booking>())
        {
            token.ThrowIfCancellationRequested();

            book.Status = BookingStatus.Confirmed;
            book.ProcessedAt = DateTime.UtcNow;

            var result = await _storageBooking.UpdateAsync(book, token);
            if (!result.IsSuccessful)
            {
                _logger.LogWarning("Не удалось обновить бронирование {BookId} для события {EventId}. " +
                    "Возможно, данное бронирование было удалено", book.Id, book.EventId);
                continue;
            }

            _logger.LogInformation("Бронирование события {EventId} успешно обработано. Заявка с " +
                "{BookId} получила статус {Status}", book.EventId, book.Id, book.Status);
        }

        return ResultCreator.Success();
    }
}
