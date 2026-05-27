using Application.Infrastructure.Common;
using Domain.Events;

namespace Application.Infrastructure;

/// <summary>
/// Репозиторий событий
/// </summary>
public interface IEventRepository : IRepository<Event>
{
}
