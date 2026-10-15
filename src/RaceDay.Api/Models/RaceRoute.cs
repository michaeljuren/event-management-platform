namespace RaceDay.Api.Models;

/// <summary>Maps to the Routes table. Named RaceRoute to avoid clashing with ASP.NET Core's [Route] attribute.
/// A specific course at a venue that one or more categories can be run or ridden on.</summary>
public class RaceRoute
{
    public int RouteId { get; set; }

    public int VenueId { get; set; }

    public string RouteName { get; set; } = string.Empty;

    public decimal DistanceKm { get; set; }

    public int? ElevationGainM { get; set; }

    public string? MapUrl { get; set; }

    public string? StartPointDescription { get; set; }

    public string? EndPointDescription { get; set; }

    public Venue Venue { get; set; } = null!;

    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
