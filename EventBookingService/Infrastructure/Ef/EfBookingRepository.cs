using Application.Infrastructure;
using Application.Infrastructure.Common;
using Domain.Bookings;
using Infrastructure.Ef.EfRepository;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Ef;

public class EfBookingRepository(AppDbContext dbContext, IUnitOfWork efUnitOfWork)
    : EfRepository<Booking>(dbContext, efUnitOfWork, TableNames.Bookings), IBookingRepository
{
    public Task<int> GetCountActiveBookingForPersonAsync(Guid userId, CancellationToken token = default)
    {
        return Items.CountAsync(b => b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed, token);
    }

    public Task<List<Guid>> GetPendingBookingsAsync(CancellationToken token = default)
    {
        return Items.Where(b => b.Status == BookingStatus.Pending).Select(b => b.Id).ToListAsync(token);
    }
}
