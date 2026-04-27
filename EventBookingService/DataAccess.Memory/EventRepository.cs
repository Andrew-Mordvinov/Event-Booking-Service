using DataAccess.Abstract;
using DataAccess.Memory.Storage;
using Entities.Events;

namespace DataAccess.Memory;

[Obsolete("Более не актуально хранение в памяти")]
public class EventRepository : DictionaryRepository<Event>, IEventRepository
{
}
