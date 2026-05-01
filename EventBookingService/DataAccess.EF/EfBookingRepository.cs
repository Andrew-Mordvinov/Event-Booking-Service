using DataAccess.Abstract;
using DataAccess.Abstract.Common;
using DataAccess.EF.EfRepository;
using Entities.Bookings;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.EF;

public class EfBookingRepository(AppDbContext dbContext, IUnitOfWork efUnitOfWork) 
    : EfRepository<Booking>(dbContext, efUnitOfWork), IBookingRepository
{
    public Task<List<Guid>> GetPendingBookingsAsync(CancellationToken token = default)
    {
        return Items.Where(b => b.Status == BookingStatus.Pending).Select(b => b.Id).ToListAsync(token);
    }
}
