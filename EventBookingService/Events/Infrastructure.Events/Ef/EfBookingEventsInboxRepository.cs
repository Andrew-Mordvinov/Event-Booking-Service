using Application.Events.DTO.Requests;
using Application.Events.Infrastructure;

using Infrastructure.Events.ExtensionMethods;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Events.Ef;

public class EfBookingEventsInboxRepository(EventsDbContext _appDbContext) : IBookingEventsInboxRepository
{
    public Task AddAsync(BookingConfirmedRequest incoming, CancellationToken token = default)
    {
        _appDbContext.BookingConfirmed.Add(incoming.ToBookingConfirmedInboxItem());

        return Task.CompletedTask;
    }

    public Task<bool> CheckIfProcessedAsync(BookingConfirmedRequest incoming, CancellationToken token = default)
    {
        return _appDbContext.BookingConfirmed.AnyAsync(t => t.BookingId == incoming.BookingId && t.EventId == incoming.EventId, token);
    }
}
