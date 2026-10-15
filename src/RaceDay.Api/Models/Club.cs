namespace RaceDay.Api.Models;

/// <summary>An athletics or cycling club that participants may belong to.</summary>
public class Club
{
    public int ClubId { get; set; }

    public string ClubName { get; set; } = string.Empty;

    public string Province { get; set; } = string.Empty;

    public string? AffiliationNumber { get; set; }

    public string? ContactEmail { get; set; }

    public ICollection<User> Members { get; set; } = new List<User>();
}
