namespace EventBookingService.Common.Storage;

/// <summary>
/// Простая обертка над memory-коллекциями, позволяет использовать
/// LINQ и добавлять/удалять элементы
/// </summary>
public interface IStorage<T> where T : IHasId
{
    IEnumerable<T> GetAll();

    T? GetById(Guid id);

    void Add(T item);

    int Remove(Guid id);

    int Count { get; }
}
