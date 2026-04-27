using DataAccess.Abstract;
using DataAccess.EF.EfRepository;
using Entities.Bookings;

namespace DataAccess.EF;

public class EfBookingRepository(AppDbContext dbContext) : EfRepository<Booking>(dbContext), IBookingRepository
{

}
