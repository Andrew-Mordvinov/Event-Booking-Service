using Bookings.Models;
using DataAccess.Abstract;
using DataAccess.EF.EfRepository;

namespace DataAccess.EF;

public class EfBookingRepository(AppDbContext dbContext) : EfRepository<Booking>(dbContext), IBookingRepository
{

}
