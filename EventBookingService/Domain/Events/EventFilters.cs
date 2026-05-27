namespace Entities.Events;

public class EventFilters
{
    public string? Title { get; set; }

    public DateTimeOffset? From { get; set; }

    public DateTimeOffset? To { get; set; }

    public void Deconstruct(out string? title, out DateTimeOffset? from, out DateTimeOffset? to)
    {
        title = Title;
        from = From;
        to = To;
    }
}
