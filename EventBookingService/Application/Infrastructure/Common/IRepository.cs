using Domain.Interfaces;

namespace Application.Infrastructure.Common;

/// <summary>
/// Интерфейс базового хранилища данных с возможностью получения отфильтрованной коллекции
/// через дерево-фильтр
/// </summary>
public interface IRepository<T> : IBaseStorage<T>, ICanGetPage<T> where T : IHasId
{

}
