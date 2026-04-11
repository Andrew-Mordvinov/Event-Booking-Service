namespace Events.Models;

public class EventFilters
{
    public string? Title { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public void Deconstruct(out string? title, out DateTime? from, out DateTime? to)
    {
        title = Title;
        from = From;
        to = To;
    }
}
