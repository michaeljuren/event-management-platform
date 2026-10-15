namespace RaceDay.Api.Models;

/// <summary>A race day created and managed by an organizer.</summary>
public class Event
{
    public int EventId { get; set; }

    public int OrganizerId { get; set; }

    public int VenueId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Running, Walking, Cycling or Multisport.</summary>
    public string Discipline { get; set; } = string.Empty;

    public DateOnly EventDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public DateTime RegistrationOpenDate { get; set; }

    public DateTime RegistrationCloseDate { get; set; }

    /// <summary>Draft, Published, Cancelled or Completed.</summary>
    public string Status { get; set; } = EventStatus.Draft;

    public string? BannerImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Organizer { get; set; } = null!;

    public Venue Venue { get; set; } = null!;

    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
