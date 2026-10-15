using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public record UserProfileResponse(
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    DateOnly DateOfBirth,
    string Gender,
    int? ClubId,
    string? ClubName,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    DateTime CreatedAt,
    bool IsActive,
    IReadOnlyList<string> Roles)
{
    public static UserProfileResponse From(User u) => new(
        u.UserId, u.FirstName, u.LastName, u.Email, u.PhoneNumber, u.DateOfBirth, u.Gender,
        u.ClubId, u.Club?.ClubName, u.EmergencyContactName, u.EmergencyContactPhone, u.CreatedAt, u.IsActive,
        u.UserRoles.Select(ur => ur.Role.RoleName).OrderBy(r => r).ToList());
}

public class UpdateProfileRequest
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>Club the user belongs to, or null for none.</summary>
    public int? ClubId { get; set; }

    [StringLength(100)]
    public string? EmergencyContactName { get; set; }

    [Phone, StringLength(20)]
    public string? EmergencyContactPhone { get; set; }
}

public class GrantRoleRequest
{
    /// <summary>Admin, Organizer or Participant.</summary>
    [Required]
    public string RoleName { get; set; } = string.Empty;
}

public record PerformanceHistoryItem(
    int EntryId,
    int EventId,
    string EventName,
    DateOnly EventDate,
    string Discipline,
    int CategoryId,
    string CategoryName,
    decimal DistanceKm,
    TimeOnly? FinishTime,
    int? OverallPosition,
    int? CategoryPosition,
    int? GenderPosition,
    string FinishStatus);
