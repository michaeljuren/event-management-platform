namespace RaceDay.Api.Models;

/// <summary>Every account on the platform: admins, organizers and participants.</summary>
public class User
{
    public int UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>PBKDF2 hash produced by ASP.NET Core Identity's password hasher. Never plain text.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public DateOnly DateOfBirth { get; set; }

    /// <summary>Male, Female or Other.</summary>
    public string Gender { get; set; } = string.Empty;

    public int? ClubId { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public Club? Club { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();

    public ICollection<Entry> Entries { get; set; } = new List<Entry>();

    public ICollection<Result> RecordedResults { get; set; } = new List<Result>();

    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}
