using Application.Bookings.Infrastructure;
using Application.Bookings.Interfaces;
using Application.Bookings.Settings;

using Domain.Bookings;
using Domain.Bookings.Exceptions;

using Microsoft.Extensions.Options;

using Shared.Exceptions;
using Shared.Infrastructure.Abstract;
using Shared.Infrastructure.Abstract.Enums;

namespace Application.Bookings.Implementation;

public class BookingService(
    IBookingRepository _bookingRepository,
    IUnitOfWork _unitOfWork,
    IUserContext _userContext,
    IOptions<BookingSettings> options) : IBookingService
{
    private readonly int _maxBookingPerUser = options.Value.MaxBookingPerUser ?? throw new ArgumentNullException("Не удалось инициализировать MaxBookingPerUser");

    public async Task<Booking> GetBookingByIdAsync(
        Guid bookingId,
        CancellationToken token = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId, GetMode.Readonly, token) ?? throw new NotFoundException(BookingServiceErrors.BookingNotFound(bookingId));

        if (booking.UserId != _userContext.UserId
            && !await _userContext.IsAdmin(token))
        {
            throw new BookingOwnershipException(BookingServiceErrors.BookingAccessDenied(bookingId));
        }

        return booking;
    }

    public async Task<Booking> CreateBookingAsync(
        Guid eventId,
        CancellationToken token = default)
    {
        var activeCount = await _bookingRepository.GetCountActiveBookingForPersonAsync(_userContext.UserId, token);

        if (activeCount >= _maxBookingPerUser)
        {
            throw new BookingLimitExceededException(BookingServiceErrors.ExceedBookingLimit(_maxBookingPerUser));
        }

        var booking = new Booking(Guid.NewGuid(), eventId, _userContext.UserId, BookingStatus.Pending, DateTime.UtcNow);

        await _bookingRepository.AddAsync(booking, token);

        await _unitOfWork.SaveChangesAsync(token);

        return booking;
    }

    // TODO возможно здесь тоже нужно какое-то событие публиковать, хотя в задании не сказано
    public async Task CancelBookingAsync(
        Guid bookingId,
        CancellationToken token = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId, GetMode.Edit, token) ?? throw new NotFoundException(BookingServiceErrors.BookingNotFound(bookingId));

        if (booking.Status is BookingStatus.Cancelled)
        {
            await _unitOfWork.RollbackChangesAsync(token);
            throw new InvalidBookingOperationException(BookingServiceErrors.BookingAlreadyCancelled(bookingId));
        }

        if (booking.Status is BookingStatus.Rejected)
        {
            await _unitOfWork.RollbackChangesAsync(token);
            throw new InvalidBookingOperationException(BookingServiceErrors.BookingRejected(bookingId));
        }

        if (booking.UserId != _userContext.UserId
            && !await _userContext.IsAdmin(token))
        {
            await _unitOfWork.RollbackChangesAsync(token);
            throw new BookingOwnershipException(BookingServiceErrors.BookingAccessDenied(bookingId));
        }

        booking.Cancel();
        // При отмене бронирования по идее тоже надо место освобождать?
        await _unitOfWork.SaveChangesAsync(token);
    }
}
