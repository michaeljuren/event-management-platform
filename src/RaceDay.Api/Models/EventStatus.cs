namespace RaceDay.Api.Models;

public static class EventStatus
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";

    public static readonly string[] All = [Draft, Published, Cancelled, Completed];
}
