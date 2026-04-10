using EventBookingService.Common;
using EventBookingService.Models.Events.Const;

namespace EventBookingService.Models.Events;

/// <summary>
/// Модель мероприятия, реализующая непосредственно бизнес-логику
/// </summary>
public class Event : IHasId, IFillable<Event>, ICopyable<Event>
{
    #region Properties

    public Guid Id { get; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime StartAt { get; protected set; }
    public DateTime EndAt { get; protected set; }

    #endregion

    #region Constructors

    public Event(Guid id, string title, DateTime start, DateTime end, string? description = null)
    {
        if (start > end)
        {
            throw new ArgumentException(EventErrors.StartAfterEndForbidden, nameof(start));
        }
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(EventErrors.TitleNeed, nameof(title));
        }
        Id = id;
        Title = title;
        Description = description ?? string.Empty;
        StartAt = start;
        EndAt = end;
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
    /// <param name="description">Описание</param>
    /// <returns>Созданный объект или null и список возникших ошибок</returns>
    public static (Event? value, IEnumerable<string> errors) TryCreate(Guid id, string? title, DateTime? start, DateTime? end, string? description = null)
    {
        try
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

            if (start is not null && end is not null && start > end)
            {
                errors.Add(EventErrors.StartAfterEndForbidden);
            }

            if (errors.Count != 0)
            {
                return (null, errors);
            }

            var value = new Event(id, title!, start.GetValueOrDefault(), end.GetValueOrDefault(), description);

            return (value, []);
        }
        catch (Exception e)
        {
            return (null, [e.Message]);
        }
    }

    #endregion

    #region Public methods

    public void FillFrom(Event source)
    {
        Title = source.Title;
        Description = source.Description;
        StartAt = source.StartAt;
        EndAt = source.EndAt;
    }

    public Event Copy() => new(Id, Title, StartAt, EndAt, Description);

    public bool Equivalent(Event other) => 
        Title == other.Title && Description == other.Description && StartAt == other.StartAt && EndAt == other.EndAt;

    #endregion
}
