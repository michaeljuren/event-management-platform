using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public record VenueResponse(
    int VenueId, string VenueName, string? AddressLine, string City, string Province,
    string? PostalCode, decimal? Latitude, decimal? Longitude)
{
    public static VenueResponse From(Venue v) =>
        new(v.VenueId, v.VenueName, v.AddressLine, v.City, v.Province, v.PostalCode, v.Latitude, v.Longitude);
}

public class VenueRequest
{
    [Required, StringLength(150)]
    public string VenueName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? AddressLine { get; set; }

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    /// <summary>One of the nine South African provinces, e.g. "Gauteng".</summary>
    [Required, StringLength(20)]
    public string Province { get; set; } = string.Empty;

    [StringLength(10)]
    public string? PostalCode { get; set; }

    [Range(-90, 90)]
    public decimal? Latitude { get; set; }

    [Range(-180, 180)]
    public decimal? Longitude { get; set; }
}

public record RouteResponse(
    int RouteId, int VenueId, string RouteName, decimal DistanceKm, int? ElevationGainM,
    string? MapUrl, string? StartPointDescription, string? EndPointDescription)
{
    public static RouteResponse From(RaceRoute r) =>
        new(r.RouteId, r.VenueId, r.RouteName, r.DistanceKm, r.ElevationGainM, r.MapUrl, r.StartPointDescription, r.EndPointDescription);
}

public class RouteRequest
{
    [Required, StringLength(150)]
    public string RouteName { get; set; } = string.Empty;

    [Range(0.01, 9999.99)]
    public decimal DistanceKm { get; set; }

    [Range(0, int.MaxValue)]
    public int? ElevationGainM { get; set; }

    [Url, StringLength(500)]
    public string? MapUrl { get; set; }

    [StringLength(255)]
    public string? StartPointDescription { get; set; }

    [StringLength(255)]
    public string? EndPointDescription { get; set; }
}
