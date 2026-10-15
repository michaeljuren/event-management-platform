using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class CategoryRequest
{
    /// <summary>Optional route at the event's venue.</summary>
    public int? RouteId { get; set; }

    /// <summary>Age or distance class, e.g. "Under 20", "Senior", "10km" or "21km".</summary>
    [Required, StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.01, 9999.99)]
    public decimal DistanceKm { get; set; }

    [Range(0, 120)]
    public int? MinAge { get; set; }

    /// <summary>Open, Male or Female. Defaults to Open.</summary>
    [StringLength(10)]
    public string? GenderRestriction { get; set; }

    [Range(0, 999999.99)]
    public decimal EntryFee { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxParticipants { get; set; }
}

public record CategoryResponse(
    int CategoryId,
    int EventId,
    int? RouteId,
    string CategoryName,
    decimal DistanceKm,
    int? MinAge,
    string GenderRestriction,
    decimal EntryFee,
    int? MaxParticipants,
    int EntryCount,
    int? SpotsRemaining)
{
    /// <summary>Entry counts only include active entries when the Entries navigation is loaded.</summary>
    public static CategoryResponse From(Category c)
    {
        var active = c.Entries.Count(e => EntryStatus.Active.Contains(e.Status));
        return new CategoryResponse(c.CategoryId, c.EventId, c.RouteId, c.CategoryName, c.DistanceKm, c.MinAge,
            c.GenderRestriction, c.EntryFee, c.MaxParticipants, active,
            c.MaxParticipants is { } max ? Math.Max(0, max - active) : null);
    }
}
