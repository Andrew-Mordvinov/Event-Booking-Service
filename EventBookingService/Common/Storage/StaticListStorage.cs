namespace EventBookingService.Common.Storage;

/// <summary>
/// Статическое хранилище List. Любой созданный экземпляр будет ссылаться
/// на одно хранилище.
/// </summary>
public class StaticListStorage<T> : IStorage<T> where T : IHasId
{
    private static readonly List<T> list = [];

    public StaticListStorage()
    {
        
    }

    public StaticListStorage(IEnumerable<T> items) => list.AddRange(items);

    public int Count => list.Count;

    public void Add(T item) => list.Add(item);

    public IEnumerable<T> GetAll() => list;

    public T? GetById(Guid id) => list.FirstOrDefault(t => t.Id == id);

    public int Remove(Guid id) => list.RemoveAll(t => t.Id == id);
}
