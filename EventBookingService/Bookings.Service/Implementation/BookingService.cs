using Bookings.Models;
using DataAccess.Storage;
using Events.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Locking;

namespace Bookings.Service.Implementation;
// TODO
// Дописать тесты для обработки броней
// Проверить работу веба
// Дописать readme
public class BookingService(
    [FromKeyedServices("Mem")] IStorage<Booking> _storageBooking,
    [FromKeyedServices("Mem")] IStorage<Event> _storageEvent,
    ILogger<BookingService> _logger,
    [FromKeyedServices("CreateBooking")] ISemaphoreGetter _createBookingSemaphore,
    [FromKeyedServices("ProcessBooking")] ISemaphoreGetter _processBookingSemaphore) : IBookingService
{
    private static readonly int _imitationDelay = 2000;

    public Task<Booking?> GetBookingByIdAsync(
        Guid bookingId,
        CancellationToken token = default) =>
        _storageBooking.GetByIdAsync(bookingId, token);

    public async Task<Booking?> CreateBookingAsync(
        Guid eventId,
        CancellationToken token = default)
    {
        await _createBookingSemaphore.SemaphoreSlim.WaitAsync(token);
        try
        {
            var entity = await _storageEvent.GetByIdAsync(eventId, token) ?? throw new NotFoundException(BookingServiceErrors.EventNotFound(eventId));
            if (!entity.TryReserveSeats())
            {
                throw new ConflictException(BookingServiceErrors.NoAvailableSeats);
            }

            var booking = new Booking(Guid.NewGuid(), eventId, BookingStatus.Pending, DateTime.UtcNow);

            // Сначала букинг сохраним, потому что откатить апдейт ивента на текущий момент проблематично, а удалить айтем из хранилища проще
            await _storageBooking.AddAsync(booking, token);

            var eventUpdateResult = await _storageEvent.UpdateAsync(entity, token);
            // Примитивная отмена изменений в хранилище, если вдруг апдейт упал (хотя если кто-то удалил событие,
            // пока мы создавали бронь, то вернется false без ошибок, и тогда бронь обработается в фоне). 
            if (!eventUpdateResult)
            {
                // Пока не думаем о том, что RemoveAsync может упасть
                await _storageBooking.RemoveAsync(booking.Id, token);

                throw new NotFoundException(BookingServiceErrors.EventNotFound(eventId));
            }

            return booking;
        }
        finally
        {
            _createBookingSemaphore.SemaphoreSlim.Release();
        }
    }

    public async Task ProcessPendingBookingsAsync(int maxCount = 100, CancellationToken token = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCount, 1);

        var pageResult = await _storageBooking.GetPageAsync(
                b => b.Status == BookingStatus.Pending,
                1,
                maxCount,
                token);

        if (pageResult is null || pageResult.Items.Count < 1)
        {
            _logger.LogInformation("Бронирований для обработки не найдено");
            return;
        }

        _logger.LogInformation("Найдено {Count} броней для обработки", pageResult.Items.Count);

        token.ThrowIfCancellationRequested();

        var tasks = pageResult.Items.Select(booking => ProcessBookingAsync(booking, token));
        // Исключения обрабатываются внутри отдельно для каждой брони
        await Task.WhenAll(tasks);

        return;
    }

    public async Task ProcessBookingAsync(Booking booking, CancellationToken token = default)
    {
        if (booking.Status != BookingStatus.Pending)
        {
            return;
        }
        // TODO если понадобится обновить event здесь, то блокировка нас не спасет от случая обновления event
        // например при запросе Put к контроллеру. Надо подумать как решить это
        try
        {
            _logger.LogInformation("Обработка бронирования {BookId} для события {EventId}", booking.Id, booking.EventId);

            await Task.Delay(_imitationDelay, token);
            token.ThrowIfCancellationRequested();

            await _processBookingSemaphore.SemaphoreSlim.WaitAsync(token);
            try
            {
                var eventResult = await _storageEvent.GetByIdAsync(booking.EventId, token);
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

                await _storageBooking.UpdateAsync(booking, token);
                _logger.LogInformation("Обработка бронирования {BookId} для события {EventId} завершена", booking.Id, booking.EventId);
            }
            finally
            {
                _processBookingSemaphore.SemaphoreSlim.Release();
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            _logger.LogInformation("Бронирование {BookId} для события {EventId} не обработано - операция отменена", booking.Id, booking.EventId);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Произошло непредвиденное исключение при обработке {Book}", booking);
        }
    }
}
