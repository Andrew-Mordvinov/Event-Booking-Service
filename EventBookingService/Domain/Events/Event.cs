using Entities.Bookings;
using Shared.Interfaces;

namespace Entities.Events;

/// <summary>
/// Модель мероприятия, реализующая непосредственно бизнес-логику
/// </summary>
public class Event : IHasId, ICopyable<Event>
{
    #region Properties

    public Guid Id { get; protected set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset StartAt { get; protected set; }
    public DateTimeOffset EndAt { get; protected set; }
    public int TotalSeats { get; protected set; }
    public int AvailableSeats { get; protected set; }
    public List<Booking> Bookings { get; protected set; } = [];

    #endregion

    #region Constructors

    protected Event()
    {
        // Для Ef Core. Разделение сущностей довольно геморройное занятие, так как change tracker работать не будет
        // Поэтому пока что сущность будет одна
    }

    internal Event(
        Guid id,
        string title,
        DateTimeOffset start,
        DateTimeOffset end,
        int totalSeats,
        int? availableSeats = null,
        string? description = null)
    {
        Id = id;
        Title = title;
        Description = description ?? string.Empty;
        StartAt = start;
        EndAt = end;
        TotalSeats = totalSeats;
        AvailableSeats = availableSeats ?? totalSeats;
    }

    #endregion

    #region Public static methods

    /// <summary>
    /// Статический метод, который пытается создать объект <see cref="Event"/>
    /// </summary>
    /// <param name="id">Идентификатор</param>
    /// <param name="title">Название</param>
    /// <param name="start">Дата начала</param>
    /// <param name="end">Дата окончания</param>
    /// <param name="totalSeats">Общее число мест</param>
    /// <param name="availableSeats">Доступное число мест. Допустимо не передавать, тогда по умолчанию будет равно общему числу мест</param>
    /// <param name="description">Описание</param>
    /// <returns>Созданный объект или null и список возникших ошибок</returns>
    public static (Event? value, IEnumerable<string> errors) TryCreate(
        Guid id,
        string? title,
        DateTimeOffset? start,
        DateTimeOffset? end,
        int? totalSeats,
        int? availableSeats = null,
        string? description = null)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add(EventErrors.TitleNeed);
        }

        if (start is null)
        {
            errors.Add(EventErrors.StartDateNeed);
        }

        if (end is null)
        {
            errors.Add(EventErrors.EndDateNeed);
        }

        if (start is not null
            && end is not null
            && start > end)
        {
            errors.Add(EventErrors.StartAfterEndForbidden);
        }

        if (totalSeats is null || totalSeats < 1)
        {
            errors.Add(EventErrors.TotalSeatsMustPositive);
        }

        if (availableSeats is not null && availableSeats < 0)
        {
            errors.Add(EventErrors.AvailableSeatsMustPositive);
        }

        if (availableSeats is not null
            && availableSeats > 0 
            && totalSeats > 0 
            && totalSeats < availableSeats)
        {
            errors.Add(EventErrors.TotalSeatsCantBeLessAvailableSeats);
        }

        if (errors.Count != 0)
        {
            return (null, errors);
        }

        var value = new Event(
            id,
            title!,
            start.GetValueOrDefault(),
            end.GetValueOrDefault(),
            totalSeats.GetValueOrDefault(),
            availableSeats,
            description);

        return (value, []);
    }

    #endregion

    #region Public methods

    public bool TryReserveSeats(int count = 1)
    {
        if (count < 1)
        {
            return false;
        }

        if (AvailableSeats < count)
        {
            return false;
        }

        AvailableSeats -= count;

        return true;
    }

    public bool TryReleaseSeats(int count = 1)
    {
        if (count < 1)
        {
            return false;
        }

        if ((AvailableSeats + count) > TotalSeats)
        {
            return false;
        }

        AvailableSeats += count;

        return true;
    }

    public void FillFrom(Event source)
    {
        Title = source.Title;
        Description = source.Description;
        StartAt = source.StartAt;
        EndAt = source.EndAt;
        TotalSeats = source.TotalSeats;
        AvailableSeats = source.AvailableSeats;
    }

    public Event Copy() => new(Id, Title, StartAt, EndAt, TotalSeats, AvailableSeats, Description);

    public bool Equivalent(Event other) => 
        Title == other.Title 
        && Description == other.Description 
        && StartAt == other.StartAt 
        && EndAt == other.EndAt
        && TotalSeats == other.TotalSeats
        && AvailableSeats == other.AvailableSeats;

    #endregion
}