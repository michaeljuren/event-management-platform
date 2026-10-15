namespace RaceDay.Api.Models;

/// <summary>Lookup of the roles a user account can hold (Admin, Organizer, Participant).</summary>
public class Role
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
