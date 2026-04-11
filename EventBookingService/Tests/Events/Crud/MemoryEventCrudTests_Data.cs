using DTO.Events.Requests;
using Events.Models;

namespace Tests.Events.Crud;

public partial class MemoryEventCrudTests
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

        public static readonly Guid BadId = Guid.Parse("678c2e1a-004f-2afc-b124-1ccc2a4c50ef");

        public static readonly Guid LiveMusicBar = Guid.Parse("9d2f5c8a-3e6b-4f1a-9c7d-2b4e8a1f5c9d");
        public static readonly Guid ReadingClub = Guid.Parse("5c9f2e7a-4d3b-4f1a-9c8d-2b6e1a5f8c4d");
    }

    /// <summary>
    /// Лист с событиями для тестирования операций удаления/модификации/получения/создания
    /// </summary>
    private readonly static IEnumerable<Event> BasicEventList =
    [
        new Event
        (
            EventIds.RockFestival,
            "Рок-фестиваль 'Красная Площадь'",
            new DateTime(2026, 3, 19, 18, 0, 0),
            new DateTime(2026, 3, 19, 23, 0, 0),
            10000,
            description: "Выступление лучших рок-групп города"
        ),
        new Event
        (
            EventIds.SymphonyConcert,
            "Концерт симфонического оркестра",
            new DateTime(2026, 3, 19, 19, 0, 0),
            new DateTime(2026, 3, 19, 21, 30, 0),
            120,
            description: "Исполнение классических произведений Чайковского"
        ),
        new Event
        (
            EventIds.ArtExhibition,
            "Выставка современного искусства",
            new DateTime(2026, 3, 20, 11, 0, 0),
            new DateTime(2026, 3, 20, 20, 0, 0),
            250,
            description: "Работы молодых художников из 10 стран мира"
        ),
        new Event
        (
            EventIds.MovieMarathon,
            "Ночной киномарафон",
            new DateTime(2026, 3, 20, 22, 0, 0),
            new DateTime(2026, 3, 21, 6, 0, 0),
            140,
            description: "Показ культовых фильмов под открытым небом"
        ),
        new Event
        (
            EventIds.FoodFestival,
            "Фестиваль уличной еды",
            new DateTime(2026, 3, 21, 12, 0, 0),
            new DateTime(2026, 3, 21, 22, 0, 0),
            75,
            description: "Дегустация блюд от лучших фудтраков города"
        ),
        new Event
        (
            EventIds.StandupEvening,
            "Вечер стендап-комедии",
            new DateTime(2026, 3, 21, 20, 0, 0),
            new DateTime(2026, 3, 21, 22, 30, 0),
            50,
            description: "Выступление популярных комиков"
        ),
        new Event
        (
            EventIds.PotteryMasterclass,
            "Мастер-класс по гончарному делу",
            new DateTime(2026, 3, 22, 14, 0, 0),
            new DateTime(2026, 3, 22, 17, 0, 0),
            4,
            description: "Создай свою керамическую кружку"
        ),
        new Event
        (
            EventIds.JazzNight,
            "Джазовый квартирник",
            new DateTime(2026, 3, 22, 19, 0, 0),
            new DateTime(2026, 3, 22, 22, 0, 0),
            12,
            description: "Уютный вечер с живой музыкой"
        ),
        new Event
        (
            EventIds.TheaterPlay,
            "Спектакль 'Вишневый сад'",
            new DateTime(2026, 3, 23, 18, 30, 0),
            new DateTime(2026, 3, 23, 21, 0, 0),
            220,
            description: "Премьера в городском театре"
        ),
        new Event
        (
            EventIds.CraftFair,
            "Ярмарка мастеров",
            new DateTime(2026, 3, 23, 10, 0, 0),
            new DateTime(2026, 3, 23, 19, 0, 0),
            35,
            description: null
        )
    ];

    #endregion

    #region GetEvent

    public static IEnumerable<object?[]> GetEvent_ExistingEventId =>
    [
        [BasicEventList, EventIds.TheaterPlay, BasicEventList.FirstOrDefault(t => t.Id == EventIds.TheaterPlay)]
    ];

    public static IEnumerable<object?[]> GetEvent_BadId =>
    [
        [BasicEventList, EventIds.BadId],
        [Enumerable.Empty<Event>(), EventIds.BadId]
    ];

    #endregion

    #region CreateEvent

    public static IEnumerable<object?[]> CreateEvent_ValidModel =>
    [
        [
            BasicEventList,
            new CreateEventRequest 
            {
                Title = "Живая музыка в баре 'Ноты и Кофе'",
                StartAt = new DateTime(2026, 3, 24, 20, 0, 0),
                EndAt = new DateTime(2026, 3, 25, 03, 0, 0),
                TotalSeats = 50,
                Description = "Акустический вечер с местными группами. Вход свободный"       
            },
            new Event
            (
                Guid.NewGuid(),
                "Живая музыка в баре 'Ноты и Кофе'",
                new DateTime(2026, 3, 24, 20, 0, 0),
                new DateTime(2026, 3, 25, 03, 0, 0),
                50,
                description: "Акустический вечер с местными группами. Вход свободный"
            )
        ],
        [
            BasicEventList,
            new CreateEventRequest
            {
                Title = "Живая музыка в баре 'Ноты и Кофе'",
                StartAt = new DateTime(2026, 3, 24, 20, 0, 0),
                EndAt = new DateTime(2026, 3, 25, 03, 0, 0),
                TotalSeats = 35,
                Description = null
            },
            new Event
            (
                Guid.NewGuid(),
                "Живая музыка в баре 'Ноты и Кофе'",
                new DateTime(2026, 3, 24, 20, 0, 0),
                new DateTime(2026, 3, 25, 03, 0, 0),
                35,
                description: null
            )
        ],
        [
            Enumerable.Empty<Event>(),
            new CreateEventRequest
            {
                Title = "Книжный клуб: обсуждение 'Мастера и Маргариты'",
                StartAt = new DateTime(2026, 3, 26, 18, 30, 0),
                EndAt = new DateTime(2026, 3, 26, 20, 30, 0),
                TotalSeats = 20,
                Description = "Встреча любителей литературы. Обсуждаем роман Булгакова за чашечкой чая"
            },
            new Event
            (
                Guid.NewGuid(),
                "Книжный клуб: обсуждение 'Мастера и Маргариты'",
                new DateTime(2026, 3, 26, 18, 30, 0),
                new DateTime(2026, 3, 26, 20, 30, 0),
                20,
                description: "Встреча любителей литературы. Обсуждаем роман Булгакова за чашечкой чая"
            )
        ],
        [
            Enumerable.Empty<Event>(),
            new CreateEventRequest
            {
                Title = "Книжный клуб: обсуждение 'Мастера и Маргариты'",
                StartAt = new DateTime(2026, 3, 25, 03, 0, 0),
                EndAt = new DateTime(2026, 3, 25, 03, 0, 0),
                TotalSeats = 1,
                Description = null
            },
            new Event
            (
                Guid.NewGuid(),
                "Книжный клуб: обсуждение 'Мастера и Маргариты'",
                new DateTime(2026, 3, 25, 03, 0, 0),
                new DateTime(2026, 3, 25, 03, 0, 0),
                1,
                description: null
            )
        ],
    ];

    public static IEnumerable<object?[]> CreateEvent_InvalidModel =>
    [
        // Название null
        [
            Enumerable.Empty<Event>(),
            new CreateEventRequest
            {
                Title = null,
                StartAt = new DateTime(2026, 3, 24, 20, 0, 0),
                EndAt = new DateTime(2026, 3, 25, 03, 0, 0),
                TotalSeats = 10,
                Description = "Акустический вечер с местными группами. Вход свободный"
            },
            new List<string> 
            {
                EventErrors.TitleNeed
            }
        ],
        // Название пустая строка
        [
            Enumerable.Empty<Event>(),
            new CreateEventRequest
            {
                Title = "",
                StartAt = new DateTime(2026, 3, 24, 20, 0, 0),
                EndAt = new DateTime(2026, 3, 25, 03, 0, 0),
                TotalSeats = 50,
                Description = "Акустический вечер с местными группами. Вход свободный"
            },
            new List<string>
            {
                EventErrors.TitleNeed
            }
        ],
        // Название только пробел
        [
            Enumerable.Empty<Event>(),
            new CreateEventRequest
            {
                Title = " ",
                StartAt = new DateTime(2026, 3, 24, 20, 0, 0),
                EndAt = new DateTime(2026, 3, 25, 03, 0, 0),
                TotalSeats = 50,
                Description = "Акустический вечер с местными группами. Вход свободный"
            },
            new List<string>
            {
                EventErrors.TitleNeed
            }
        ],
        // Нет обеих дат
        [
            Enumerable.Empty<Event>(),
            new CreateEventRequest
            {
                Title = "Книжный клуб: обсуждение 'Мастера и Маргариты'",
                StartAt = null,
                EndAt = null,
                TotalSeats = 9,
                Description = null
            },
            new List<string>
            {
                EventErrors.StartDateNeed,
                EventErrors.EndDateNeed
            }
        ],
        // Дата начала пуста
        [
            Enumerable.Empty<Event>(),
            new CreateEventRequest
            {
                Title = "Книжный клуб: обсуждение 'Мастера и Маргариты'",
                StartAt = null,
                EndAt = new DateTime(2026, 3, 25, 03, 0, 0),
                TotalSeats = 9,
                Description = null
            },
            new List<string>
            {
                EventErrors.StartDateNeed
            }
        ],
        // Дата окончания пуста
        [
            Enumerable.Empty<Event>(),
            new CreateEventRequest
            {
                Title = "Книжный клуб: обсуждение 'Мастера и Маргариты'",
                StartAt = new DateTime(2026, 3, 25, 03, 0, 0),
                EndAt = null,
                TotalSeats = 9,
                Description = null
            },
            new List<string>
            {
                EventErrors.EndDateNeed
            }
        ],
        // Дата начала позже даты окончания
        [
            Enumerable.Empty<Event>(),
            new CreateEventRequest
            {
                Title = "Книжный клуб: обсуждение 'Мастера и Маргариты'",
                StartAt = new DateTime(2026, 3, 25, 04, 0, 0),
                EndAt = new DateTime(2026, 3, 25, 03, 0, 0),
                TotalSeats = 9,
                Description = null
            },
            new List<string>
            {
                EventErrors.StartAfterEndForbidden
            }
        ],
        // Не передано число мест
        [
            Enumerable.Empty<Event>(),
            new CreateEventRequest
            {
                Title = "Книжный клуб: обсуждение 'Мастера и Маргариты'",
                StartAt = new DateTime(2026, 3, 25, 04, 0, 0),
                EndAt = new DateTime(2026, 3, 26, 03, 0, 0),
                TotalSeats = null,
                Description = null
            },
            new List<string>
            {
                EventErrors.TotalSeatsMustPositive
            }
        ],
        // Число мест 0
        [
            Enumerable.Empty<Event>(),
            new CreateEventRequest
            {
                Title = "Книжный клуб: обсуждение 'Мастера и Маргариты'",
                StartAt = new DateTime(2026, 3, 25, 04, 0, 0),
                EndAt = new DateTime(2026, 3, 26, 03, 0, 0),
                TotalSeats = 0,
                Description = null
            },
            new List<string>
            {
                EventErrors.TotalSeatsMustPositive
            }
        ],
        // Число мест меньше 0
        [
            Enumerable.Empty<Event>(),
            new CreateEventRequest
            {
                Title = "Книжный клуб: обсуждение 'Мастера и Маргариты'",
                StartAt = new DateTime(2026, 3, 25, 04, 0, 0),
                EndAt = new DateTime(2026, 3, 26, 03, 0, 0),
                TotalSeats = 0,
                Description = null
            },
            new List<string>
            {
                EventErrors.TotalSeatsMustPositive
            }
        ],
        // Полностью пустой объект
        [
            Enumerable.Empty<Event>(),
            new CreateEventRequest
            {
                Title = null,
                StartAt = null,
                EndAt = null,
                TotalSeats = null,
                Description = null
            },
            new List<string>
            {
                EventErrors.TitleNeed,
                EventErrors.StartDateNeed,
                EventErrors.EndDateNeed,
                EventErrors.TotalSeatsMustPositive
            }
        ],
    ];

    #endregion

    #region DeleteEvent

    public static IEnumerable<object?[]> DeleteEvent_ExistingEventId =>
    [
        [
            BasicEventList,
            EventIds.JazzNight
        ]
    ];

    public static IEnumerable<object?[]> DeleteEvent_BadId =>
    [
        [
            BasicEventList,
            EventIds.BadId
        ],
        [
            Enumerable.Empty<Event>(),
            EventIds.BadId
        ],
    ];

    #endregion

    #region ModifyEvent

    public static IEnumerable<object?[]> ModifyEvent_ValidDataAndId =>
    [
        [
            BasicEventList,
            EventIds.JazzNight,
            new ModifyEventRequest
            {
                Title = "Вечер джазовой музыки",
                StartAt = new DateTime(2026, 3, 25, 21, 0, 0),
                EndAt = new DateTime(2026, 3, 26, 01, 0, 0),
                TotalSeats = 69,
                Description = null
            },
            new Event
            (
                EventIds.JazzNight,
                "Вечер джазовой музыки",
                new DateTime(2026, 3, 25, 21, 0, 0),
                new DateTime(2026, 3, 26, 01, 0, 0),
                69,
                description: null
            )
        ],
        [
            BasicEventList,
            EventIds.CraftFair,
            new ModifyEventRequest
            {
                Title = "Ярмарка мастеров",
                StartAt = new DateTime(2026, 3, 23, 10, 0, 0),
                EndAt = new DateTime(2026, 3, 23, 19, 0, 0),
                TotalSeats = 10,
                Description = string.Empty
            },
            new Event
            (
                EventIds.CraftFair,
                "Ярмарка мастеров",
                new DateTime(2026, 3, 23, 10, 0, 0),
                new DateTime(2026, 3, 23, 19, 0, 0),
                10,
                description: null
            )
        ],
        [
            BasicEventList,
            EventIds.FoodFestival,
            new ModifyEventRequest
            {
                Title = "Фестиваль уличной еды. Вход свободный",
                StartAt = new DateTime(2026, 3, 25, 03, 0, 0),
                EndAt = new DateTime(2026, 3, 25, 03, 0, 0),
                TotalSeats = 150,
                Description = null
            },
            new Event
            (
                EventIds.FoodFestival,
                "Фестиваль уличной еды. Вход свободный",
                new DateTime(2026, 3, 25, 03, 0, 0),
                new DateTime(2026, 3, 25, 03, 0, 0),
                150,
                description: null
            )
        ],
    ];

    public static IEnumerable<object?[]> ModifyEvent_ValidDataAndBadId =>
    [
        [
            BasicEventList,
            EventIds.BadId,
            new ModifyEventRequest
            {
                Title = "Книжный клуб: обсуждение 'Мастера и Маргариты'",
                StartAt = new DateTime(2026, 3, 24, 20, 0, 0),
                EndAt = new DateTime(2026, 3, 25, 03, 0, 0),
                TotalSeats = 15,
                Description = null
            }
        ],
        [
            BasicEventList,
            EventIds.BadId,
            new ModifyEventRequest
            {
                Title = "Книжный клуб: обсуждение 'Мастера и Маргариты'",
                StartAt = new DateTime(2026, 3, 24, 20, 0, 0),
                EndAt = new DateTime(2026, 3, 25, 03, 0, 0),
                TotalSeats = 1,
                Description = string.Empty
            }
        ],
        [
            Enumerable.Empty<Event>(),
            EventIds.BadId,
            new ModifyEventRequest
            {
                Title = "Книжный клуб: обсуждение 'Мастера и Маргариты'",
                StartAt = new DateTime(2026, 3, 24, 20, 0, 0),
                EndAt = new DateTime(2026, 3, 25, 03, 0, 0),
                TotalSeats = 150,
                Description = "Акустический вечер с местными группами. Вход свободный"
            }
        ],
    ];

    public static IEnumerable<object?[]> ModifyEvent_InvalidData =>
    [
        // Название null
        [
            BasicEventList,
            EventIds.FoodFestival,
            new ModifyEventRequest
            {
                Title = null,
                StartAt = new DateTime(2026, 3, 24, 20, 0, 0),
                EndAt = new DateTime(2026, 3, 25, 03, 0, 0),
                TotalSeats = 15,
                Description = "Акустический вечер с местными группами. Вход свободный"
            },
            new List<string>
            {
                EventErrors.TitleNeed
            }
        ],
        // Название пустая строка
        [
            BasicEventList,
            EventIds.FoodFestival,
            new ModifyEventRequest
            {
                Title = string.Empty,
                StartAt = new DateTime(2026, 3, 24, 20, 0, 0),
                EndAt = new DateTime(2026, 3, 25, 03, 0, 0),
                TotalSeats = 15,
                Description = "Акустический вечер с местными группами. Вход свободный"
            },
            new List<string>
            {
                EventErrors.TitleNeed
            }
        ],
        // Название только пробел
        [
            BasicEventList,
            EventIds.FoodFestival,
            new ModifyEventRequest
            {
                Title = " ",
                StartAt = new DateTime(2026, 3, 24, 20, 0, 0),
                EndAt = new DateTime(2026, 3, 25, 03, 0, 0),
                TotalSeats = 15,
                Description = "Акустический вечер с местными группами. Вход свободный"
            },
            new List<string>
            {
                EventErrors.TitleNeed
            }
        ],
        // Нет обеих дат
        [
            BasicEventList,
            EventIds.FoodFestival,
            new ModifyEventRequest
            {
                Title = "Книжный клуб: обсуждение 'Мастера и Маргариты'",
                StartAt = null,
                EndAt = null,
                TotalSeats = 15,
                Description = null
            },
            new List<string>
            {
                EventErrors.StartDateNeed,
                EventErrors.EndDateNeed
            }
        ],
        // Дата начала пуста
        [
            BasicEventList,
            EventIds.FoodFestival,
            new ModifyEventRequest
            {
                Title = "Книжный клуб: обсуждение 'Мастера и Маргариты'",
                StartAt = null,
                EndAt = new DateTime(2026, 3, 25, 03, 0, 0),
                TotalSeats = 15,
                Description = null
            },
            new List<string>
            {
                EventErrors.StartDateNeed
            }
        ],
        // Дата окончания пуста
        [
            BasicEventList,
            EventIds.FoodFestival,
            new ModifyEventRequest
            {
                Title = "Книжный клуб: обсуждение 'Мастера и Маргариты'",
                StartAt = new DateTime(2026, 3, 25, 03, 0, 0),
                EndAt = null,
                TotalSeats = 15,
                Description = null
            },
            new List<string>
            {
                EventErrors.EndDateNeed
            }
        ],
        // Дата начала позже даты окончания
        [
            BasicEventList,
            EventIds.FoodFestival,
            new ModifyEventRequest
            {
                Title = "Книжный клуб: обсуждение 'Мастера и Маргариты'",
                StartAt = new DateTime(2026, 3, 25, 12, 0, 0),
                EndAt = new DateTime(2026, 3, 24, 15, 0, 0),
                TotalSeats = 15,
                Description = null
            },
            new List<string>
            {
                EventErrors.StartAfterEndForbidden
            }
        ],
        // Количество мест 0
        [
            BasicEventList,
            EventIds.FoodFestival,
            new ModifyEventRequest
            {
                Title = "Книжный клуб: обсуждение 'Мастера и Маргариты'",
                StartAt = new DateTime(2026, 3, 24, 20, 0, 0),
                EndAt = new DateTime(2026, 3, 25, 03, 0, 0),
                TotalSeats = 0,
                Description = null
            },
            new List<string>
            {
                EventErrors.TotalSeatsMustPositive
            }
        ],
        // Количество мест меньше 0
        [
            BasicEventList,
            EventIds.FoodFestival,
            new ModifyEventRequest
            {
                Title = "Книжный клуб: обсуждение 'Мастера и Маргариты'",
                StartAt = new DateTime(2026, 3, 24, 20, 0, 0),
                EndAt = new DateTime(2026, 3, 25, 03, 0, 0),
                TotalSeats = -10,
                Description = null
            },
            new List<string>
            {
                EventErrors.TotalSeatsMustPositive
            }
        ],
        // Полностью пустой объект
        [
            BasicEventList,
            EventIds.FoodFestival,
            new ModifyEventRequest
            {
                Title = null,
                StartAt = null,
                EndAt = null,
                TotalSeats = null,
                Description = null
            },
            new List<string>
            {
                EventErrors.TitleNeed,
                EventErrors.StartDateNeed,
                EventErrors.EndDateNeed,
                EventErrors.TotalSeatsMustPositive
            }
        ],
    ];

    #endregion
}
