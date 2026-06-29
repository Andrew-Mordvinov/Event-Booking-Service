using Domain.Bookings;
using Microsoft.EntityFrameworkCore;
using Application.Bookings.Infrastructure;
using Infrastructure.Bookings.Ef;
using Shared.Interfaces.Infrastructure;
using Shared.Infrastructure.Ef;

namespace Infrastructure.Bookings.Ef;

public class EfBookingRepository(BookingsDbContext dbContext, IUnitOfWork efUnitOfWork)
    : EfRepository<Booking, BookingsDbContext>(dbContext, efUnitOfWork, TableNames.Bookings), IBookingRepository
{
    public Task<int> GetCountActiveBookingForPersonAsync(Guid userId, CancellationToken token = default)
    {
        return Items.CountAsync(
            b => 
                (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed) 
                && b.UserId == userId,
            token);
    }

    public Task<List<Guid>> GetPendingBookingsAsync(CancellationToken token = default)
    {
        return Items.Where(b => b.Status == BookingStatus.Pending).Select(b => b.Id).ToListAsync(token);
    }
}
