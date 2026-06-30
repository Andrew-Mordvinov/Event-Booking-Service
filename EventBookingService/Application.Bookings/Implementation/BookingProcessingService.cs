using Application.Bookings.Infrastructure;
using Application.Bookings.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Abstract;

namespace Application.Bookings.Implementation;

public class BookingProcessingService(
    IBookingRepository _storageBooking,
    IBookingEventsProducer _eventProducer,
    IUnitOfWork _unitOfWork,
    ILogger<BookingProcessingService> _logger) : IBookingProcessingService
{
    private const int _imitationDelay = 2000;

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

            booking.Confirm();
            _logger.LogInformation("Бронирование события {EventId} успешно обработано. Заявка с " +
                "{BookId} получила статус {Status}", booking.EventId, booking.Id, booking.Status);

            await _eventProducer.BookingConfirmedAsync(booking, token);

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
