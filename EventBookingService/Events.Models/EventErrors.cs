namespace Events.Models;

public static class EventErrors
{
    public const string TitleNeed = "Название мероприятия обязательно. Пустые строки или строки только с пробелами недопустимы";
    public const string StartDateNeed = "Дата начала обязательна";
    public const string EndDateNeed = "Дата окончания обязательна";
    public const string StartAfterEndForbidden = "Дата начала события не может быть позже даты окончания";
    public const string TotalSeatsMustPositive = "Общее число мест у события должно быть больше нуля";
    public const string AvailableSeatsMustPositive = "Доступное число мест у события должно быть больше или равно нулю";
    public const string TotalSeatsCantBeLessAvailableSeats = "Доступное число мест у события не должно превышать общее";
}
