using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class RegisterRequest
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>At least 8 characters with an upper-case letter, a lower-case letter and a digit.</summary>
    [Required, StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Password must contain an upper-case letter, a lower-case letter and a digit.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>Male, Female or Other.</summary>
    [Required, StringLength(10)]
    public string Gender { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>Organizer or Participant (the British spelling "Organiser" is also accepted).</summary>
    [Required]
    public string Role { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public record UserSummaryResponse(int UserId, string FirstName, string LastName, string Email, IReadOnlyList<string> Roles);

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    UserSummaryResponse User);
