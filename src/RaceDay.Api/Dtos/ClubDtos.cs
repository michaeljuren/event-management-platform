using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public record ClubResponse(int ClubId, string ClubName, string Province, string? AffiliationNumber, string? ContactEmail)
{
    public static ClubResponse From(Club c) => new(c.ClubId, c.ClubName, c.Province, c.AffiliationNumber, c.ContactEmail);
}

public class ClubRequest
{
    [Required, StringLength(150)]
    public string ClubName { get; set; } = string.Empty;

    /// <summary>One of the nine South African provinces, e.g. "Western Cape".</summary>
    [Required, StringLength(20)]
    public string Province { get; set; } = string.Empty;

    [StringLength(30)]
    public string? AffiliationNumber { get; set; }

    [EmailAddress, StringLength(255)]
    public string? ContactEmail { get; set; }
}

public class ClubQuery : PageQuery
{
    /// <summary>Optional province filter.</summary>
    public string? Province { get; set; }
}
