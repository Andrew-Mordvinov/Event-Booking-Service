using Bookings.Models;
using DataAccess.Abstract.Common;

namespace DataAccess.Abstract;

/// <summary>
/// Репозиторий бронирований
/// </summary>
public interface IBookingRepository : IRepository<Booking>
{
}
