using DataAccess.Abstract.Common;
using Entities.Bookings;

namespace DataAccess.Abstract;

/// <summary>
/// Репозиторий бронирований
/// </summary>
public interface IBookingRepository : IRepository<Booking>
{
}
