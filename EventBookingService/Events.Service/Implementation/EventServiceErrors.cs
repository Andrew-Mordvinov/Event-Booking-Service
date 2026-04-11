namespace Events.Service.Implementation;

public static class EventServiceErrors
{
    public const string InvalidPageNumber = "Некорректное значение номера страницы: номер не должен быть меньше 1";

    public static string PageSizeOutOfRange(int pageMin, int pageMax) =>
        $"Некорректное значение размера страницы: размер должен быть в диапазоне {pageMin}-{pageMax}";
}
