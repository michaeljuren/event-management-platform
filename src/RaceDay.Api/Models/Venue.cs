namespace RaceDay.Api.Models;

/// <summary>A physical location that events are held at.</summary>
public class Venue
{
    public int VenueId { get; set; }

    public string VenueName { get; set; } = string.Empty;

    public string? AddressLine { get; set; }

    public string City { get; set; } = string.Empty;

    public string Province { get; set; } = string.Empty;

    public string? PostalCode { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public ICollection<RaceRoute> Routes { get; set; } = new List<RaceRoute>();

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
