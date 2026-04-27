using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

using Shared;

using Swashbuckle.AspNetCore.Annotations;

namespace DTO.Presentation.Events.Requests;

public class GetEventsRequest
{
    private const int _pageDefault = 1;
    private const int _pageDefaultSize = 10;

    /// <summary>
    /// Фильтр по полю Title
    /// </summary>
    [FromQuery(Name = "title")]
    public string? Title { get; set; }

    /// <summary>
    /// Фильтр по полю StartAt (>= from)
    /// </summary>
    [FromQuery(Name = "from")]
    public DateTimeOffset? From { get; set; }

    /// <summary>
    /// Фильтр по полю EndAt (<= to)
    /// </summary>
    [FromQuery(Name = "to")]
    public DateTimeOffset? To { get; set; }

    /// <summary>
    /// Номер страницы
    /// </summary>
    [FromQuery(Name = "page")]
    [Range(1, int.MaxValue, ErrorMessage = "Номер страницы должен быть больше 0")]
    public int? Page { get; set; }

    /// <summary>
    /// Размер страницы
    /// </summary>
    [FromQuery(Name = "pageSize")]
    [Range(GlobalConst.MinPageSize, GlobalConst.MaxPageSize, ErrorMessage = "Размер страницы должен быть от {0} до {1}")]
    public int? PageSize { get; set; }

    [SwaggerIgnore]
    public int EffectivePage => Page ?? _pageDefault;

    [SwaggerIgnore]
    public int EffectivePageSize => PageSize ?? _pageDefaultSize;
}
