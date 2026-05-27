using DataAccess.Abstract.Common;
using Entities.Events;

namespace DataAccess.Abstract;

/// <summary>
/// Репозиторий событий
/// </summary>
public interface IEventRepository : IRepository<Event> 
{
}
