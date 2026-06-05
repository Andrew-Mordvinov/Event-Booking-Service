using Application.Infrastructure;
using Application.Infrastructure.Common;
using Domain.Bookings;
using Infrastructure.Ef.EfRepository;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Ef;

public class EfBookingRepository(AppDbContext dbContext, IUnitOfWork efUnitOfWork)
    : EfRepository<Booking>(dbContext, efUnitOfWork, TableNames.Bookings), IBookingRepository
{
    public Task<List<Guid>> GetPendingBookingsAsync(CancellationToken token = default)
    {
        return Items.Where(b => b.Status == BookingStatus.Pending).Select(b => b.Id).ToListAsync(token);
    }
}
