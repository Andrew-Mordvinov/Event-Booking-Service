using DataAccess.Abstract;
using DataAccess.Memory.Storage;
using Entities.Bookings;

namespace DataAccess.Memory;

[Obsolete("Более не актуально хранение в памяти")]
public class BookingRepository : DictionaryRepository<Booking>, IBookingRepository
{
}
