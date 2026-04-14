using Bookings.Models;
using DataAccess.Storage;
using Events.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Locking;
using Validation;

namespace Bookings.Service.Implementation;
// TODO
// Дописать тесты для обработки броней
// Проверить работу веба
// Дописать readme
public class BookingService(
    IStorage<Booking> _storageBooking,
    IStorage<Event> _storageEvent,
    ILogger<BookingService> _logger,
    [FromKeyedServices("CreateBooking")]ISemaphoreGetter _createBookingSemaphore,
    [FromKeyedServices("ProcessBooking")] ISemaphoreGetter _processBookingSemaphore) : IBookingService
{
    private static readonly int _imitationDelay = 2000;

    public Task<ValidationResult<Booking?>> GetBookingByIdAsync(
        Guid bookingId,
        CancellationToken token = default) =>
        _storageBooking.GetByIdAsync(bookingId, token);

    public async Task<ValidationResult<Booking?>> CreateBookingAsync(
        Guid eventId,
        CancellationToken token = default)
    {
        await _createBookingSemaphore.SemaphoreSlim.WaitAsync(token);
        try
        {
            var eventResult = await _storageEvent.GetByIdAsync(eventId, token);

            if (!eventResult.IsSuccessful)
            {
                return ResultCreator.Fail<Booking?>(null, eventResult.Errors);
            }

            if (eventResult.Value is null)
            {
                return ResultCreator.Success<Booking?>(null);
            }

            if (!eventResult.Value.TryReserveSeats())
            {
                return ResultCreator.Fail<Booking?>(null, new ValidationItem(BookingServiceErrors.NoAvailableSeats, ItemCategory.ConflictError));
            }

            var booking = new Booking(Guid.NewGuid(), eventId, BookingStatus.Pending, DateTime.UtcNow);

            // Сначала букинг сохраним, потому что откатить апдейт ивента на текущий момент проблематично, а удалить айтем из хранилища проще
            var bookingResult = await _storageBooking.AddAsync(booking, token);

            if (!bookingResult.IsSuccessful)
            {
                return bookingResult.ToGeneric<Booking?>(null);
            }

            var eventUpdateResult = await _storageEvent.UpdateAsync(eventResult.Value, token);
            // Примитивная отмена изменений в хранилище, если вдруг апдейт упал (хотя если кто-то удалил событие,
            // пока мы создавали бронь, то вернется false без ошибок, и тогда бронь обработается в фоне). 
            if (!eventUpdateResult.IsSuccessful)
            {
                // Пока не думаем о том, что RemoveAsync может упасть
                await _storageBooking.RemoveAsync(booking.Id, token);

                return ResultCreator.Fail<Booking?>(null, eventUpdateResult.Errors);
            }

            return ResultCreator.Success(booking);
        }
        finally
        {
            _createBookingSemaphore.SemaphoreSlim.Release();
        }
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

        if (pageResult.Value is null || pageResult.Value.Items.Count < 1)
        {
            _logger.LogInformation("Бронирований для обработки не найдено");
            return ResultCreator.Success();
        }

        _logger.LogInformation("Найдено {Count} броней для обработки", pageResult.Value.Items.Count);

        token.ThrowIfCancellationRequested();

        var tasks = pageResult.Value.Items.Select(booking => ProcessBookingAsync(booking, token));
        // Исключения обрабатываются внутри отдельно для каждой брони
        await Task.WhenAll(tasks);

        return ResultCreator.Success();
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

                if (eventResult.Value is null)
                {
                    booking.Reject();
                    _logger.LogWarning("Событие {EventId} не удалось получить. Бронь {BookId} отклонена.", booking.Id, booking.EventId);
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
