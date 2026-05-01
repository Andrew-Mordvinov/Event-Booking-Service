using DataAccess.EF.EfRepository;
using static Tests.TemplateRepository.SharedTestData;

namespace Tests.EfInfrastructure;

public partial class EfRepositoryTests
{
    public static readonly IEnumerable<object?[]> GetPageAsync_ValidFilterAndPageParams =
    [
        [
            BaseListForFilter,
            Filters.Positive,
            1,
            5,
            new[]
            {
                TestItemIds.Eighth,
                TestItemIds.Fourth,
                TestItemIds.Ninth,
                TestItemIds.Twelfth,
                TestItemIds.Seventh
            },
            8,
            2
        ],
        [
            BaseListForFilter,
            null,
            1,
            5,
            new[]
            {
                TestItemIds.Eighth,
                TestItemIds.Eleventh,
                TestItemIds.Fourth,
                TestItemIds.Ninth,
                TestItemIds.Twelfth
            },
            12,
            3
        ],
        [
            BaseListForFilter,
            Filters.Positive,
            2,
            5,
            new[]
            {
                TestItemIds.Tenth,
                TestItemIds.First,
                TestItemIds.Fifth
            },
            8,
            2
        ],
        [
            BaseListForFilter,
            Filters.PositiveAndText,
            1,
            5,
            new[]
            {
                TestItemIds.First,
                TestItemIds.Twelfth
            },
            2,
            1
        ],
        [
            BaseListForFilter,
            Filters.PositiveAndNotText,
            1,
            5,
            new[]
            {
                TestItemIds.Eighth,
                TestItemIds.Fourth,
                TestItemIds.Ninth,
                TestItemIds.Seventh,
                TestItemIds.Tenth
            },
            6,
            2
        ],
        [
            BaseListForFilter,
            Filters.PositiveAndNotText,
            2,
            5,
            new[]
            {
                TestItemIds.Fifth
            },
            6,
            2
        ],
        [
            BaseListForFilter,
            Filters.RangeAndText,
            1,
            5,
            new[]
            {
                TestItemIds.Seventh,
                TestItemIds.Eighth
            },
            2,
            1
        ],
        [
            BaseListForFilter,
            Filters.LargeOrNegative,
            1,
            10,
            new[]
            {
                TestItemIds.Eleventh,
                TestItemIds.Sixth,
                TestItemIds.Tenth,
                TestItemIds.Second,
                TestItemIds.Fifth
            },
            5,
            1
        ],
        [
            BaseListForFilter,
            Filters.LargeOrNegative,
            2,
            2,
            new[]
            {
                TestItemIds.Tenth,
                TestItemIds.Second
            },
            5,
            3
        ],
        [
            BaseListForFilter,
            Filters.ExactOrEmpty,
            1,
            5,
            new[]
            {
                TestItemIds.Ninth
            },
            1,
            1
        ]
    ];
    public static IEnumerable<object?[]> GetPageAsync_NoElementAfterFilter => TestGetPage_NoElementAfterFilter;

    public static IEnumerable<object?[]> GetPageAsync_BadPaging =>
    [
        // По сути дублирование данных из теста mem хранилища, но ошибки в других константах
        // Да и в целом набор ошибок куда менее универсальный, чем данные + фильтр + пейджинг, поэтому разделение адекватное
        [
            BaseListForFilter,
            Filters.ExactOrEmpty,
            -2,
            10,
            new string[]
            {
                EfRepositoryErrors.PageMustBePositive
            }
        ],

        [
            BaseListForFilter,
            null,
            0,
            0,
            new string[]
            {
                EfRepositoryErrors.PageMustBePositive,
                EfRepositoryErrors.PageSizeMustBePositive
            }
        ],

        [
            BaseListForFilter,
            Filters.TextEqualsText,
            1,
            -1,
            new string[]
            {
                EfRepositoryErrors.PageSizeMustBePositive
            }
        ],

        [
            BaseListForFilter,
            null,
            1,
            -1,
            new string[]
            {
                EfRepositoryErrors.PageSizeMustBePositive
            }
        ],

        [
            BaseListForFilter,
            Filters.Positive,
            3,
            5,
            new string[]
            {
                EfRepositoryErrors.PageNotFound(3, 2)
            }
        ],

        [
            BaseListForFilter,
            null,
            2,
            15,
            new string[]
            {
                EfRepositoryErrors.PageNotFound(2, 1)
            }
        ],
    ];
}
