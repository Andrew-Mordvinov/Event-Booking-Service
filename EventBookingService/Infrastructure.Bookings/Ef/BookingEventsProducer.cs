using Application.Bookings.Infrastructure;
using Domain.Bookings;
using Infrastructure.Bookings.ExtensionMethods;

namespace Infrastructure.Bookings.Ef;

/// <summary>
/// Реализация продюсера событий, работающего с Outbox таблицей
/// </summary>
public class BookingEventsProducer(BookingsDbContext _dbContext) : IBookingEventsProducer
{
    public Task BookingConfirmedAsync(Booking booking, CancellationToken token)
    {
        _dbContext.BookingConfirmed.Add(booking.ToBookingConfirmedOutboxItem());

        return Task.CompletedTask;
    }
}
