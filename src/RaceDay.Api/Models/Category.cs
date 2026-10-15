namespace RaceDay.Api.Models;

/// <summary>An age or distance class within an event, e.g. "10km Individual" or "Under 20".</summary>
public class Category
{
    public int CategoryId { get; set; }

    public int EventId { get; set; }

    public int? RouteId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public decimal DistanceKm { get; set; }

    public int? MinAge { get; set; }

    /// <summary>Open, Male or Female.</summary>
    public string GenderRestriction { get; set; } = Gender.Open;

    public decimal EntryFee { get; set; }

    public int? MaxParticipants { get; set; }

    public Event Event { get; set; } = null!;

    public RaceRoute? Route { get; set; }

    public ICollection<Entry> Entries { get; set; } = new List<Entry>();
}
