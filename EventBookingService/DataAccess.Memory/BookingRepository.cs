using Bookings.Models;
using DataAccess.Abstract;
using DataAccess.Memory.Storage;

namespace DataAccess.Memory;

[Obsolete("Более не актуально хранение в памяти")]
public class BookingRepository : DictionaryRepository<Booking>, IBookingRepository
{
}
