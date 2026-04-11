namespace Shared.Interfaces;

/// <summary>
/// Простой интерфейс для объектов, которые можно идентифицировать по публичному Id
/// </summary>
public interface IHasId
{
    Guid Id { get; }
}
