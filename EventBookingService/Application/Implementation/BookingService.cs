using Application.Infrastructure;
using Application.Infrastructure.Common;
using Application.Infrastructure.Enums;
using Application.Interfaces;
using Domain.Bookings;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Application.Implementation;

public class BookingService(
    IBookingRepository _storageBooking,
    IEventRepository _storageEvent,
    IUnitOfWork _unitOfWork,
    ILogger<BookingService> _logger) : IBookingService
{
    private const int _imitationDelay = 2000;

    public Task<Booking?> GetBookingByIdAsync(
        Guid bookingId,
        CancellationToken token = default) =>
        _storageBooking.GetByIdAsync(bookingId, GetMode.Readonly, token);

    public async Task<Booking> CreateBookingAsync(
        Guid eventId,
        CancellationToken token = default)
    {
        var entity = await _storageEvent.GetByIdAsync(eventId, token: token) ?? throw new NotFoundException(BookingServiceErrors.EventNotFound(eventId));
        if (!entity.TryReserveSeats())
        {
            throw new ConflictException(BookingServiceErrors.NoAvailableSeats);
        }

        var booking = new Booking(Guid.NewGuid(), eventId, BookingStatus.Pending, DateTime.UtcNow);

        await _storageBooking.AddAsync(booking, token);

        await _unitOfWork.SaveChangesAsync(token);

        return booking;
    }

    public async Task ProcessBookingAsync(Guid bookingId, CancellationToken token = default)
    {
        try
        {
            _logger.LogInformation("Обработка бронирования {BookId}", bookingId);

            await Task.Delay(_imitationDelay, token);
            token.ThrowIfCancellationRequested();

            var booking = await _storageBooking.GetByIdAsync(bookingId, token: token);
            token.ThrowIfCancellationRequested();

            if (booking is null)
            {
                _logger.LogInformation("Бронирование {BookId} не найдено в хранилище. Возможно оно было удалено", bookingId);
                return;
            }

            var eventResult = await _storageEvent.GetByIdAsync(booking.EventId, token: token);
            token.ThrowIfCancellationRequested();

            if (eventResult is null)
            {
                booking.Reject();
                _logger.LogWarning("Событие {EventId} не удалось получить. Бронь {BookId} отклонена.", booking.EventId, booking.Id);
            }
            else
            {
                booking.Confirm();
                _logger.LogInformation("Бронирование события {EventId} успешно обработано. Заявка с " +
                    "{BookId} получила статус {Status}", booking.EventId, booking.Id, booking.Status);
            }

            await _unitOfWork.SaveChangesAsync(token);
            _logger.LogInformation("Обработка бронирования {BookId} для события {EventId} завершена", booking.Id, booking.EventId);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            _logger.LogInformation("Бронирование {BookId} не обработано - операция отменена", bookingId);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Произошло непредвиденное исключение при обработке бронирования с идентификатором {BookId}", bookingId);
        }
    }
}
