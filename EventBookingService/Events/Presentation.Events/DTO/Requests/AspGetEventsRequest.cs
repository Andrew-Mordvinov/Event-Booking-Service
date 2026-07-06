using Domain.Events;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace Presentation.Events.DTO.Requests;

public class AspGetEventsRequest
{
    private const int _pageDefault = 1;
    private const int _pageDefaultSize = 10;

    /// <summary>
    /// Фильтр по полю Title
    /// </summary>
    [FromQuery(Name = "title")]
    public string? Title { get; init; }

    /// <summary>
    /// Фильтр по полю StartAt (Дата начала). Больше или равна from
    /// </summary>
    [FromQuery(Name = "from")]
    public DateTimeOffset? From { get; init; }

    /// <summary>
    /// Фильтр по полю EndAt (Дата окончания). Меньше или равна to
    /// </summary>
    [FromQuery(Name = "to")]
    public DateTimeOffset? To { get; init; }

    /// <summary>
    /// Номер страницы
    /// </summary>
    [FromQuery(Name = "page")]
    [Range(1, int.MaxValue, ErrorMessage = "Номер страницы должен быть больше 0")]
    public int? Page { get; init; }

    /// <summary>
    /// Размер страницы
    /// </summary>
    [FromQuery(Name = "pageSize")]
    [Range(GlobalConst.MinPageSize, GlobalConst.MaxPageSize, ErrorMessage = "Размер страницы должен быть от {0} до {1}")]
    public int? PageSize { get; init; }

    [SwaggerIgnore]
    public int EffectivePage => Page ?? _pageDefault;

    [SwaggerIgnore]
    public int EffectivePageSize => PageSize ?? _pageDefaultSize;
}
