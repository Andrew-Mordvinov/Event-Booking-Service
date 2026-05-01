using DataAccess.Memory.Storage;
using Tests.TemplateRepository;
using static Tests.TemplateRepository.SharedTestData;

namespace Tests.Storage;

public partial class DictionaryStorageTests
{
    public static IEnumerable<object?[]> GetPageAsync_ValidFilterAndPageParams => TestGetPage_ValidParams;

    public static IEnumerable<object?[]> GetPageAsync_NoElementAfterFilter => TestGetPage_NoElementAfterFilter;

    public static IEnumerable<object?[]> GetPageAsync_BadPaging =>
    [
        [
            BaseListForFilter,
            Filters.ExactOrEmpty,
            -2,
            10,
            new string[] 
            {
                DictionaryRepoErrors.PageMustBePositive
            }
        ],

        [
            BaseListForFilter,
            null,
            0,
            0,
            new string[]
            {
                DictionaryRepoErrors.PageMustBePositive,
                DictionaryRepoErrors.PageSizeMustBePositive
            }
        ],

        [
            BaseListForFilter,
            Filters.TextEqualsText,
            1,
            -1,
            new string[]
            {
                DictionaryRepoErrors.PageSizeMustBePositive
            }
        ],

        [
            BaseListForFilter,
            null,
            1,
            -1,
            new string[]
            {
                DictionaryRepoErrors.PageSizeMustBePositive
            }
        ],

        [
            BaseListForFilter,
            Filters.Positive,
            3,
            5,
            new string[]
            {
                DictionaryRepoErrors.PageNotFound(3, 2)
            }
        ],

        [
            BaseListForFilter,
            null,
            2,
            15,
            new string[]
            {
                DictionaryRepoErrors.PageNotFound(2, 1)
            }
        ],
    ];
}
