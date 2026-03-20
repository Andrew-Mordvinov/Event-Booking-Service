namespace EventBookingService.Models.Events.Const;

public static class EventErrors
{
    public const string TitleNeed = "Название мероприятия обязательно. Пустые строки или строки только с пробелами недопустимы";
    public const string StartDateNeed = "Дата начала обязательна";
    public const string EndDateNeed = "Дата окончания обязательна";
    public const string StartAfterEndForbidden = "Дата начала события не может быть позже даты окончания";
}
