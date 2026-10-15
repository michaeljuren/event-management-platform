namespace RaceDay.Api.Models;

/// <summary>Many-to-many link: a user can be an Organizer and a Participant at the same time.</summary>
public class UserRole
{
    public int UserId { get; set; }

    public int RoleId { get; set; }

    public User User { get; set; } = null!;

    public Role Role { get; set; } = null!;
}
