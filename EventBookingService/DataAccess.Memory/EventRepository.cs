using DataAccess.Abstract;
using DataAccess.Memory.Storage;
using Events.Models;

namespace DataAccess.Memory;

[Obsolete("Более не актуально хранение в памяти")]
public class EventRepository : DictionaryRepository<Event>, IEventRepository
{
}
