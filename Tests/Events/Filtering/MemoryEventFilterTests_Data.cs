using EventBookingService.Application.Events.Implementation;
using EventBookingService.Common;
using EventBookingService.Models.Events;

namespace Tests.Events.Filtering;

public partial class MemoryEventFilterTests
{
    #region Common fields

    private static class EventIds
    {
        public static readonly Guid RockFestival = Guid.Parse("7a9c3e48-5f2d-4b1a-9e8f-2c6d9b1a4e3f");
        public static readonly Guid SymphonyConcert = Guid.Parse("b5d8f21a-3c6e-49d0-8b7f-1a9e4d2c8b5a");
        public static readonly Guid ArtExhibition = Guid.Parse("4f2e9c1a-7d3b-4a8f-b6c2-9e5d1a8f4c7b");
        public static readonly Guid MovieMarathon = Guid.Parse("8d3b5f1a-2c6e-4a9d-b8f4-1e7c2a9d4b6f");
        public static readonly Guid FoodFestival = Guid.Parse("1c9e4f2a-5b8d-4a3f-9e7c-2d6b1a4f8c9e");
        public static readonly Guid StandupEvening = Guid.Parse("6b2d4f1a-9c8e-4a3d-b7f5-1e2c8a9d4b6f");
        public static readonly Guid PotteryMasterclass = Guid.Parse("3f7a1c9e-4d2b-4a6f-b8c5-9e2d1a4f7c8b");
        public static readonly Guid JazzNight = Guid.Parse("9c4e2b1a-5d8f-4a3c-b7f6-1e9d2a4c8b5f");
        public static readonly Guid TheaterPlay = Guid.Parse("2d5b8f1a-6c3e-4a9d-b7f4-1e8c2a9d4b6f");
        public static readonly Guid CraftFair = Guid.Parse("5a8c2e1b-7d4f-4a3c-b9f6-2e1d8a4c7b9f");
        public static readonly Guid LiveMusicBar = Guid.Parse("9d2f5c8a-3e6b-4f1a-9c7d-2b4e8a1f5c9d");
        public static readonly Guid ReadingClub = Guid.Parse("5c9f2e7a-4d3b-4f1a-9c8d-2b6e1a5f8c4d");
    }

    /// <summary>
    /// Лист с событиями для тестирования операций фильтрации
    /// </summary>
    private static IEnumerable<Event> BasicEventList =
    [
        new Event
        (
            EventIds.RockFestival,
            "Рок-фестиваль 'Красная Площадь'",
            new DateTime(2026, 3, 19, 18, 0, 0),
            new DateTime(2026, 3, 19, 23, 0, 0),
            "Выступление лучших рок-групп города"
        ),
        new Event
        (
            EventIds.SymphonyConcert,
            "Концерт симфонического оркестра",
            new DateTime(2026, 3, 19, 19, 0, 0),
            new DateTime(2026, 3, 19, 21, 30, 0),
            "Исполнение классических произведений Чайковского"
        ),
        new Event
        (
            EventIds.ArtExhibition,
            "Выставка современного искусства",
            new DateTime(2026, 3, 19, 12, 0, 0),
            new DateTime(2026, 3, 20, 0, 0, 0),
            "Работы молодых художников из 10 стран мира"
        ),
        new Event
        (
            EventIds.MovieMarathon,
            "Ночной киномарафон",
            new DateTime(2026, 3, 20, 0, 0, 0),
            new DateTime(2026, 3, 20, 6, 0, 0),
            "Показ культовых фильмов под открытым небом"
        ),
        new Event
        (
            EventIds.FoodFestival,
            "Фестиваль уличной еды",
            new DateTime(2026, 3, 21, 12, 0, 0),
            new DateTime(2026, 3, 21, 22, 0, 0),
            "Дегустация блюд от лучших фудтраков города"
        ),
        new Event(
            EventIds.StandupEvening,
            "Вечер стендап-комедии",
            new DateTime(2026, 3, 21, 20, 0, 0),
            new DateTime(2026, 3, 22, 0, 0, 0),
            "Выступление популярных комиков"
        ),
        new Event
        (
            EventIds.PotteryMasterclass,
            "Мастер-класс по гончарному делу",
            new DateTime(2026, 3, 22, 14, 0, 0),
            new DateTime(2026, 3, 22, 17, 0, 0),
            "Создай свою керамическую кружку"
        ),
        new Event
        (
            EventIds.JazzNight,
            "Джазовый концерт",
            new DateTime(2026, 3, 22, 19, 0, 0),
            new DateTime(2026, 3, 22, 22, 0, 0),
            "Уютный вечер с живой музыкой"
        ),
        new Event
        (
            EventIds.TheaterPlay,
            "Спектакль 'Вишневый сад'",
            new DateTime(2026, 3, 23, 18, 30, 0),
            new DateTime(2026, 3, 23, 21, 0, 0),
            "Премьера в городском театре"
        ),
        new Event
        (
            EventIds.CraftFair,
            "Ярмарка мастеров",
            new DateTime(2026, 3, 23, 10, 0, 0),
            new DateTime(2026, 3, 23, 19, 0, 0),
            null
        ),
        new Event
        (
            EventIds.LiveMusicBar,
            "Живая музыка в баре 'Ноты и Кофе'",
            new DateTime(2026, 4, 1, 20, 0, 0),
            new DateTime(2026, 4, 1, 23, 0, 0),
            "Акустический вечер с местными группами. Вход свободный"
        ),
        new Event
        (
            EventIds.ReadingClub,
            "Книжный клуб: обсуждение 'Мастера и Маргариты'",
            new DateTime(2026, 4, 2, 18, 30, 0),
            new DateTime(2026, 4, 2, 20, 30, 0),
            "Встреча любителей литературы. Обсуждаем роман Булгакова за чашечкой чая"
        )
    ];

    #endregion

    #region FilterEvent_AllParamExistAndValid

    public static IEnumerable<object?[]> FilterEvent_ParamValid =>
    [
        // 1. Поиск только по названию (обычный регистр)
        [
            BasicEventList,
            new EventFilters { Title = "фестиваль" },
            1, 10,
            2, // expectedCount
            1, // expectedPageCount
            new[] { EventIds.RockFestival, EventIds.FoodFestival } // ожидаемые ID
        ],   
        // 2. Поиск только по названию (ВЕРХНИЙ РЕГИСТР) - проверка регистронезависимости
        [
            BasicEventList,
            new EventFilters { Title = "ФЕСТИВАЛЬ" },
            1, 10,
            2,
            1,
            new[] { EventIds.RockFestival, EventIds.FoodFestival }
        ],
        // 3. Поиск только по начальной дате (с 22 марта)
        [
            BasicEventList,
            new EventFilters { From = new DateTime(2026, 3, 22) },
            1, 10,
            6,
            1,
            new[]
            {
                EventIds.PotteryMasterclass, // 22.03
                EventIds.JazzNight,           // 22.03
                EventIds.TheaterPlay,          // 23.03
                EventIds.CraftFair,            // 23.03
                EventIds.LiveMusicBar,         // 01.04
                EventIds.ReadingClub           // 02.04
            }
        ],
        // 4. Поиск только по конечной дате (до 20 марта включительно)
        [
            BasicEventList,
            new EventFilters { To = new DateTime(2026, 3, 20) },
            1, 10,
            3,
            1,
            new[]
            {
                EventIds.RockFestival,     // 19.03
                EventIds.SymphonyConcert,   // 19.03
                EventIds.ArtExhibition,     // 20.03 граничное
            }
        ],
        // 5. Поиск по названию + конечной дате (фестивали до конца марта)
        [
            BasicEventList,
            new EventFilters
            {
                Title = "фестиваль",
                To = new DateTime(2026, 3, 31)
            },
            1, 10,
            2,
            1,
            new[] { EventIds.RockFestival, EventIds.FoodFestival }
        ],
        // 6. Поиск по названию + начальной дате (концерты с 22 марта)
        [
            BasicEventList,
            new EventFilters
            {
                Title = "концерт",
                From = new DateTime(2026, 3, 22)
            },
            1, 10,
            1,
            1,
            new[] { EventIds.JazzNight } // Только джазовый концерт 22.03
        ],
        // 7. Поиск по начальной + конечной дате (события 21-22 марта)
        [
            BasicEventList,
            new EventFilters
            {
                From = new DateTime(2026, 3, 21),
                To = new DateTime(2026, 3, 22)
            },
            1, 10,
            2,
            1,
            new[]
            {
                EventIds.FoodFestival,       // 21.03
                EventIds.StandupEvening,     // 21.03
            }
        ],
        // 8. Поиск по всем трём фильтрам (фестивали 19-21 марта)
        [
            BasicEventList,
            new EventFilters
            {
                Title = "фестиваль",
                From = new DateTime(2026, 3, 21),
                To = new DateTime(2026, 3, 22)
            },
            1, 10,
            1,
            1,
            new[] { EventIds.FoodFestival }
        ],
        // 9. Пустой фильтр - все события, но на странице только 10
        [
            BasicEventList,
            new EventFilters { Title = null, From = null, To = null },
            1, 10,
            12,
            2,
            new[]
            {
                EventIds.RockFestival,
                EventIds.SymphonyConcert,
                EventIds.ArtExhibition,
                EventIds.MovieMarathon,
                EventIds.FoodFestival,
                EventIds.StandupEvening,
                EventIds.PotteryMasterclass,
                EventIds.JazzNight,
                EventIds.TheaterPlay,
                EventIds.CraftFair
            }
        ],
        // 10. Пагинация: вторая страница (первые 5 элементов на странице 1)
        [
            BasicEventList,
            new EventFilters(),
            2, 5,
            12,
            3,
            new[]
            {
                EventIds.StandupEvening,      // 6-й
                EventIds.PotteryMasterclass,  // 7-й
                EventIds.JazzNight,           // 8-й
                EventIds.TheaterPlay,         // 9-й
                EventIds.CraftFair            // 10-й
            }
        ],
        // 11. Пагинация: последняя страница (оставшиеся 2 элемента)
        [
            BasicEventList,
            new EventFilters(),
            3, 5,
            12,
            3,
            new[]
            {
                EventIds.LiveMusicBar,  // 11-й
                EventIds.ReadingClub  // 12-й
            }
        ]
    ];

    #endregion

    #region FilterEvent_BadPagingParam

    public static IEnumerable<object?[]> FilterEvent_BadPagingParam =>
    [
        // Page = 0
        [
            BasicEventList,
            new EventFilters(),
            0,
            10,
            new List<string>
            {
                MemoryEventServiceErrors.InvalidPageNumber
            }
        ],    
        // Page отрицательное
        [
            BasicEventList,
            new EventFilters(),
            -1,
            10,
            new List<string>
            {
                MemoryEventServiceErrors.InvalidPageNumber
            }
        ],
        // PageSize = 0
        [
            BasicEventList,
            new EventFilters(),
            1,
            0,
            new List<string>
            {
                MemoryEventServiceErrors.PageSizeOutOfRange(GlobalConst.MinPageSize, GlobalConst.MaxPageSize)
            }
        ],
        // PageSize больше максимального
        [
            BasicEventList,
            new EventFilters(),
            0,
            101,
            new List<string>
            {
                MemoryEventServiceErrors.InvalidPageNumber,
                MemoryEventServiceErrors.PageSizeOutOfRange(GlobalConst.MinPageSize, GlobalConst.MaxPageSize)
            }
        ],
        // PageSize отрицательное
        [
            BasicEventList,
            new EventFilters(),
            1,
            -5,
            new List<string>
            {
                MemoryEventServiceErrors.PageSizeOutOfRange(GlobalConst.MinPageSize, GlobalConst.MaxPageSize)
            }
        ]
    ];

    #endregion

    #region FilterEvent_PageGreaterThanMaxPage

    public static IEnumerable<object?[]> FilterEvent_PageGreaterThanMaxPage =>
    [
        // Всего 12 событий, pageSize = 5, всего страниц = 3
        [
            BasicEventList,
            new EventFilters(),
            4, // page > totalPages (3)
            5, // pageSize
            new List<string>
            {
                MemoryEventServiceErrors.PageNotFound(4, 3)
            }
        ],
        
        // С фильтром, который уменьшает количество результатов (2 фестиваля, pageSize=5 => 1 страница)
        [
            BasicEventList,
            new EventFilters { Title = "фестиваль" },
            2, // page > totalPages (1)
            5, // pageSize
            new List<string>
            {
                MemoryEventServiceErrors.PageNotFound(2, 1)
            }
        ],
        
        // С фильтром по дате (апрельские события, 2 шт, pageSize=1 => 2 страницы)
        [
            BasicEventList,
            new EventFilters { From = new DateTime(2026, 4, 1) },
            4, // page > totalPages (2)
            1, // pageSize
            new List<string>
            {
                MemoryEventServiceErrors.PageNotFound(4, 2)
            }
        ]
    ];

    #endregion

    #region FilterEvent_NoElementsAfterFilter

    public static IEnumerable<object?[]> FilterEvent_NoElementsAfterFilter =>
    [
        // Фильтр по несуществующему названию
        [
            BasicEventList,
            new EventFilters { Title = "НесуществующееСобытие" },
            1,
            10
        ],
        // Фильтр с пустым массивом
        [
            Enumerable.Empty<Event>(),
            new EventFilters { To = new DateTime(2020, 1, 1) },
            1,
            10
        ],
    ];

    #endregion
}
