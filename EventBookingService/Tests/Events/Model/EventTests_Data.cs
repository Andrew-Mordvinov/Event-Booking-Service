using Entities.Events;

namespace Tests.Events.Model;

public partial class EventTests
{
    public static IEnumerable<object?[]> TryReserveSeats_SeatsAvailable =>
    [
        [10, 1],
        [2, 2],
        [10, 3]
    ];

    public static IEnumerable<object?[]> TryReserveSeats_NotEnoughSeats =>
    [
        [10, 15],
        [1, 2]
    ];

    public static IEnumerable<object?[]> TryReserveSeats_IncorrectCount =>
    [
        [0],
        [-1],
        [-20]
    ];

    public static IEnumerable<object?[]> TryReleaseSeats_SeatsAvailable =>
    [
        [10, 9, 1],
        [2, 0, 1],
        [10, 3, 5]
    ];

    public static IEnumerable<object?[]> TryReleaseSeats_NotEnoughSeats =>
    [
        [10, 9, 2],
        [1, 0, 2],
        [5, 2, 4]
    ];

    public static IEnumerable<object?[]> TryReleaseSeats_IncorrectCount =>
    [
        [0],
        [-1],
        [-20]
    ];

    public static IEnumerable<object?[]> TryCreate_ValidParams =>
    [
        ["Название", new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), 10, null, null ],
        ["Другое название", new DateTime(2026, 1, 1), new DateTime(2026, 2, 1), 10, 5, null ],
        ["Название", new DateTime(2026, 2, 1), new DateTime(2026, 2, 5), 10, 9, "Описание" ],
    ];

    public static IEnumerable<object?[]> TryCreate_InvalidParams =>
    [
        [
            null,
            new DateTime(2026, 2, 1),
            new DateTime(2026, 1, 2),
            10,
            null,
            null,
            new string[] 
            {
                EventErrors.TitleNeed,
                EventErrors.StartAfterEndForbidden
            }
        ],
        [
            string.Empty,
            null,
            new DateTime(2026, 1, 2),
            10,
            null,
            null,
            new string[]
            {
                EventErrors.TitleNeed,
                EventErrors.StartDateNeed
            }
        ],
        [
            "Название",
            null,
            new DateTime(2026, 2, 1),
            -10,
            null,
            null,
            new string[]
            {
                EventErrors.StartDateNeed,
                EventErrors.TotalSeatsMustPositive
            }
        ],
        [
            "Название",
            new DateTime(2026, 2, 1),
            new DateTime(2026, 2, 2),
            0,
            -1,
            "Описание",
            new string[]
            {
                EventErrors.TotalSeatsMustPositive,
                EventErrors.AvailableSeatsMustPositive
            }
        ],
        [
            "Название",
            new DateTime(2026, 2, 1),
            null,
            5,
            6,
            "Описание",
            new string[]
            {
                EventErrors.TotalSeatsCantBeLessAvailableSeats,
                EventErrors.EndDateNeed
            }
        ],
        [
            null,
            null,
            null,
            null,
            null,
            null,
            new string[]
            {
                EventErrors.TitleNeed,
                EventErrors.StartDateNeed,
                EventErrors.EndDateNeed,
                EventErrors.TotalSeatsMustPositive
            }
        ],
    ];
}
