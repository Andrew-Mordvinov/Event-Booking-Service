using DataAccess.Abstract.Common;
using Events.Models;

namespace DataAccess.Abstract;

/// <summary>
/// Репозиторий событий
/// </summary>
public interface IEventRepository : IRepository<Event> 
{
}
