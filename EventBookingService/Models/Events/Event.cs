namespace EventBookingService.Models.Events;

/// <summary>
/// Модель мероприятия, реализующая непосредственно бизнес-логику
/// </summary>
public class Event
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
            throw new ArgumentException("Дата начала события не может быть позже даты окончания", nameof(start));
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
            if (title is null)
            {
                errors.Add("Название мероприятия обязательно");
            }

            if (start is null)
            {
                errors.Add("Дата начала обязательна");
            }

            if (end is null)
            {
                errors.Add("Дата окончания обязательна");
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

    /// <summary>
    /// Заполняет все поля в текущем экземпляре данными источника (кроме идентификатора)
    /// </summary>
    /// <param name="source">Источник, из которого заполняется текущий объект</param>
    public void FillFrom(Event source)
    {
        Title = source.Title;
        Description = source.Description;
        StartAt = source.StartAt;
        EndAt = source.EndAt;
    }

    /// <summary>
    /// Создает полного клона текущего объекта
    /// </summary>
    /// <returns>Новый экземпляр события</returns>
    public Event Clone() => new(Id, Title, StartAt, EndAt, Description);

    #endregion
}
