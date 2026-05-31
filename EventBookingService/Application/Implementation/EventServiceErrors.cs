namespace Application.Implementation;

public static class EventServiceErrors
{
    public const string InvalidPageNumber = "Некорректное значение номера страницы: номер не должен быть меньше 1";

    public const string UncorrectTotalSeatsToUpdate = "Невозможно установить число мест меньшее, чем уже занято";

    public static string PageSizeOutOfRange(int pageMin, int pageMax) =>
        $"Некорректное значение размера страницы: размер должен быть в диапазоне {pageMin}-{pageMax}";

    public static string EventNotFound(Guid id) => $"Событие с идентификатором {id} не найдено";
}
