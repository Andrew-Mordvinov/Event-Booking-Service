namespace EventBookingService.Application.Events.Implementation;

public static class MemoryEventServiceErrors
{
    public const string InvalidPageNumber = "Некорректное значение номера страницы: номер не должен быть меньше 1";

    public static string PageSizeOutOfRange(int pageMin, int pageMax) =>
        $"Некорректное значение размера страницы: размер должен быть в диапазоне {pageMin}-{pageMax}";

    public static string PageNotFound(int page, int totalPages) =>
        $"Указанная страница {page} не существует. Максимальная в текущем запросе страница - {totalPages}";
}
