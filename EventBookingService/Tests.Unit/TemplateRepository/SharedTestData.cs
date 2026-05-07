using System.Linq.Expressions;

namespace Tests.TemplateRepository;

public class SharedTestData
{
    public static class TestItemIds
    {
        public static readonly Guid First = Guid.Parse("c1f7a9e2-6b4d-4c8a-9f21-3d7e5a2b8c91");
        public static readonly Guid Second = Guid.Parse("a9d3c5b7-2e6f-4b1a-8c9d-5e7f2a4b6c31");
        public static readonly Guid Third = Guid.Parse("7e2c1a9b-4d6f-4a8c-b3e1-9d5a2f7c6b84");
        public static readonly Guid Fourth = Guid.Parse("3b8d5f1e-9c2a-4a7f-b6d4-1e2c9a5f8b73");
        public static readonly Guid Fifth = Guid.Parse("f2a7c9d1-5b3e-4a8c-9d6f-2c1b7e4a5d89");
        public static readonly Guid Sixth = Guid.Parse("8c1e4a7d-2b5f-4a9c-b3d6-7e2f1a5c9b48");
        public static readonly Guid Seventh = Guid.Parse("6d9b2c1f-4a7e-4c8a-b5d3-1f2e9a6c7b85");
        public static readonly Guid Eighth = Guid.Parse("1a5c9e7b-3d2f-4a8c-b6e4-9c1a2f7d5b68");
        public static readonly Guid Ninth = Guid.Parse("4e7a2c1d-9b5f-4a8c-b3d6-2c1f9e7a5b84");
        public static readonly Guid Tenth = Guid.Parse("9f2c1a7e-5b4d-4a8c-b6e3-1d2a9c7f5b68");
        public static readonly Guid Eleventh = Guid.Parse("2b7e1c9a-4d5f-4a8c-b3e6-9f1a2c7d5b84");
        public static readonly Guid Twelfth = Guid.Parse("5d1a7c9e-2b4f-4a8c-b6e3-1c2f9a7d5b68");
    }

    public static class Filters
    {
        /// <summary>
        /// x => x.IntField > 0
        /// </summary>
        public static readonly Expression<Func<TestItem, bool>> Positive =
            x => x.IntField > 0;

        /// <summary>
        /// x => x.IntField >= 0
        /// </summary>
        public static readonly Expression<Func<TestItem, bool>> NonNegative =
            x => x.IntField >= 0;

        /// <summary>
        /// x => x.IntField == 42
        /// </summary>
        public static readonly Expression<Func<TestItem, bool>> IntEquals42 =
            x => x.IntField == 42;

        /// <summary>
        /// x => x.TextField == "TEXT"
        /// </summary>
        public static readonly Expression<Func<TestItem, bool>> TextEqualsText =
            x => x.TextField == "TEXT";

        /// <summary>
        /// x => x.IntField > 100
        /// </summary>
        public static readonly Expression<Func<TestItem, bool>> LargeValues =
            x => x.IntField > 100;

        /// <summary>
        /// x => x.IntField > 0 && x.TextField == "TEXT"
        /// </summary>
        public static readonly Expression<Func<TestItem, bool>> PositiveAndText =
            x => x.IntField > 0 && x.TextField == "TEXT";

        /// <summary>
        /// x => x.IntField > 0 && x.TextField != "TEXT"
        /// </summary>
        public static readonly Expression<Func<TestItem, bool>> PositiveAndNotText =
            x => x.IntField > 0 && x.TextField != "TEXT";

        /// <summary>
        /// x => x.IntField >= 1 && x.IntField <= 50 && x.TextField.ToLower() == "filterme"
        /// </summary>
        public static readonly Expression<Func<TestItem, bool>> RangeAndText =
            x => x.IntField >= 1 && x.IntField <= 50 && x.TextField.ToLower() == "filterme";

        /// <summary>
        /// x => x.IntField > 100 || x.IntField < 0
        /// </summary>
        public static readonly Expression<Func<TestItem, bool>> LargeOrNegative =
            x => x.IntField > 100 || x.IntField < 0;

        /// <summary>
        /// x => x.IntField == 7 || x.TextField == string.Empty
        /// </summary>
        public static readonly Expression<Func<TestItem, bool>> ExactOrEmpty =
            x => x.IntField == 7 || x.TextField == string.Empty;

        /// <summary>
        /// x => x.TextField == "This text is not suit any item"
        /// </summary>
        public static readonly Expression<Func<TestItem, bool>> NoElementsAfterThis =
            x => x.TextField == "This text is not suit any item";
    }

    /// <summary>
    /// Типовой набор данных для тестирования хранилища
    /// </summary>
    public static readonly IEnumerable<TestItem> BaseListForFilter =
    [
        new()
        {
            Id = TestItemIds.First,
            IntField = 1,
            TextField = "TEXT"
        },
        new()
        {
            Id = TestItemIds.Second,
            IntField = -5,
            TextField = "text"
        },
        new()
        {
            Id = TestItemIds.Third,
            IntField = 0,
            TextField = "TeXt"
        },
        new()
        {
            Id = TestItemIds.Fourth,
            IntField = 10,
            TextField = "Another"
        },
        new()
        {
            Id = TestItemIds.Fifth,
            IntField = 999,
            TextField = "Something"
        },
        new()
        {
            Id = TestItemIds.Sixth,
            IntField = -100,
            TextField = "TEXT"
        },
        new()
        {
            Id = TestItemIds.Seventh,
            IntField = 42,
            TextField = "FilterMe"
        },
        new()
        {
            Id = TestItemIds.Eighth,
            IntField = 42,
            TextField = "filterme"
        },
        new()
        {
            Id = TestItemIds.Ninth,
            IntField = 7,
            TextField = string.Empty
        },
        new()
        {
            Id = TestItemIds.Tenth,
            IntField = int.MaxValue,
            TextField = "EDGE"
        },
        new()
        {
            Id = TestItemIds.Eleventh,
            IntField = int.MinValue,
            TextField = "edge"
        },
        new()
        {
            Id = TestItemIds.Twelfth,
            IntField = 1,
            TextField = "TEXT"
        }
    ];

    /// <summary>
    /// Общий массив данных для тестирования репозитория на получение корректных данных страницы
    /// </summary>
    public static readonly IEnumerable<object?[]> TestGetPage_ValidParams =
    [
        // 1
        [
            BaseListForFilter,
            Filters.Positive,
            1, // Номер запрашиваемой страницы
            5, // Количество элементов на странице
            new[]
            {
                TestItemIds.First,
                TestItemIds.Fourth,
                TestItemIds.Fifth,
                TestItemIds.Seventh,
                TestItemIds.Eighth
            },
            8, // Общее число элементов после фильтра
            2  // Количество страниц
        ],
        // 2
        [
            BaseListForFilter,
            null,
            1,
            5,
            new[]
            {
                TestItemIds.First,
                TestItemIds.Second,
                TestItemIds.Third,
                TestItemIds.Fourth,
                TestItemIds.Fifth
            },
            12,
            3
        ],
        // 3
        [
            BaseListForFilter,
            Filters.Positive,
            2,
            5,
            new[]
            {
                TestItemIds.Ninth,
                TestItemIds.Tenth,
                TestItemIds.Twelfth
            },
            8,
            2
        ],
        // 4
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
        // 5
        [
            BaseListForFilter,
            Filters.PositiveAndNotText,
            1,
            5,
            new[]
            {
                TestItemIds.Fourth,
                TestItemIds.Fifth,
                TestItemIds.Seventh,
                TestItemIds.Eighth,
                TestItemIds.Ninth
            },
            6,
            2
        ],
        // 6
        [
            BaseListForFilter,
            Filters.PositiveAndNotText,
            2,
            5,
            new[]
            {
                TestItemIds.Tenth
            },
            6,
            2
        ],
        // 7
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
        // 8
        [
            BaseListForFilter,
            Filters.LargeOrNegative,
            1,
            10,
            new[]
            {
                TestItemIds.Second,
                TestItemIds.Fifth,
                TestItemIds.Sixth,
                TestItemIds.Tenth,
                TestItemIds.Eleventh
            },
            5,
            1
        ],
        // 9
        [
            BaseListForFilter,
            Filters.LargeOrNegative,
            2,
            2,
            new[]
            {
                TestItemIds.Sixth,
                TestItemIds.Tenth
            },
            5,
            3
        ],
        // 10
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

    /// <summary>
    /// Общий массив для тестирования репозитория на отсутствие элементов после фильтра
    /// (или вообще, если в нем и не было элементов)
    /// </summary>
    public static readonly IEnumerable<object?[]> TestGetPage_NoElementAfterFilter =
    [
        [
            BaseListForFilter,
            Filters.NoElementsAfterThis,
            1,
            10
        ],

        [
            Enumerable.Empty<TestItem>(),
            null,
            1,
            10
        ],

        [
            Enumerable.Empty<TestItem>(),
            Filters.RangeAndText,
            1,
            10
        ],
    ];
}
