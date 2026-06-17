using Application.Infrastructure;
using Application.Infrastructure.Common;
using Application.Infrastructure.Enums;
using Application.Interfaces;
using Application.Settings;
using Domain.Bookings;
using Domain.Exceptions;
using Domain.Exceptions.Bookings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Implementation;

public class BookingService(
    IBookingRepository _storageBooking,
    IEventRepository _storageEvent,
    IUnitOfWork _unitOfWork,
    IUserContext _userContext,
    IOptions<BookingSettings> options,
    ILogger<BookingService> _logger) : IBookingService
{
    private const int _imitationDelay = 2000;

    private readonly int _maxBookingPerUser = options.Value.MaxBookingPerUser ?? throw new ArgumentNullException("Не удалось инициализировать MaxBookingPerUser");

    public async Task<Booking> GetBookingByIdAsync(
        Guid bookingId,
        CancellationToken token = default)
    {
        var booking = await _storageBooking.GetByIdAsync(bookingId, GetMode.Readonly, token) ?? throw new NotFoundException(BookingServiceErrors.BookingNotFound(bookingId));
        
        if (booking.UserId != _userContext.UserId
            && !await _userContext.IsAdmin(token))
        {
            throw new BookingOwnershipException(BookingServiceErrors.BookingAccessDenied(bookingId));
        }

        return booking;
    }

    public async Task<Booking> CreateBookingAsync(
        Guid eventId,
        Guid userId,
        CancellationToken token = default)
    {
        var activeCount = await _storageBooking.GetCountActiveBookingForPersonAsync(userId, token);
        
        if (activeCount >= _maxBookingPerUser)
        {
            throw new BookingLimitExceededException(BookingServiceErrors.ExceedBookingLimit(_maxBookingPerUser));
        }
        
        var entity = await _storageEvent.GetByIdAsync(eventId, GetMode.Edit, token: token) ?? throw new NotFoundException(BookingServiceErrors.EventNotFound(eventId));
        if (!entity.TryReserveSeats())
        {
            await _unitOfWork.RollbackChangesAsync(token);
            throw new ConflictException(BookingServiceErrors.NoAvailableSeats);
        }

        var booking = new Booking(Guid.NewGuid(), eventId, userId, BookingStatus.Pending, DateTime.UtcNow);

        await _storageBooking.AddAsync(booking, token);

        await _unitOfWork.SaveChangesAsync(token);

        return booking;
    }

    public async Task CancelBookingAsync(
        Guid bookingId,
        CancellationToken token = default)
    {
        var booking = await _storageBooking.GetByIdAsync(bookingId, GetMode.Edit, token) ?? throw new NotFoundException(BookingServiceErrors.BookingNotFound(bookingId));

        if (booking.Status is BookingStatus.Cancelled)
        {
            await _unitOfWork.RollbackChangesAsync(token);
            throw new BookingCancelledException(BookingServiceErrors.BookingAlreadyCancelled(bookingId));
        }

        if (booking.UserId != _userContext.UserId
            && !await _userContext.IsAdmin(token))
        {
            await _unitOfWork.RollbackChangesAsync(token);
            throw new BookingOwnershipException(BookingServiceErrors.BookingAccessDenied(bookingId));
        }
        // Не пользуемся свойством букинга, так как оно не защищено от параллельного доступа
        var @event = await _storageEvent.GetByIdAsync(booking.EventId, GetMode.Edit, token);

        // В теории невозможно, так как связь по внешнему ключу сейчас каскадно удаляет
        if (@event is null)
        {
            await _unitOfWork.RollbackChangesAsync(token);
            throw new NotFoundException(BookingServiceErrors.EventNotFound(booking.EventId));
        }
        @event.TryReleaseSeats();
        booking.Cancel();

        await _unitOfWork.SaveChangesAsync(token);
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
