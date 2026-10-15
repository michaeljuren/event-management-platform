using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RaceDay.Api.Authorization;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Registration, login and session management.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(
    RaceDayDbContext db,
    IPasswordService passwords,
    ITokenService tokens,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider clock) : ControllerBase
{
    /// <summary>Register a new account as an Organizer or a Participant.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<UserSummaryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var roleName = NormaliseSelfRegisterRole(request.Role);
        if (roleName is null)
        {
            ModelState.AddModelError(nameof(request.Role), "Role must be either 'Organizer' or 'Participant'.");
        }

        if (!Gender.UserValues.Contains(request.Gender))
        {
            ModelState.AddModelError(nameof(request.Gender), "Gender must be 'Male', 'Female' or 'Other'.");
        }

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (request.DateOfBirth is { } dob && (dob >= today || dob < today.AddYears(-120)))
        {
            ModelState.AddModelError(nameof(request.DateOfBirth), "Date of birth must be a real date in the past.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var email = NormaliseEmail(request.Email);
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Email already registered",
                detail: "An account with this email address already exists.");
        }

        var role = await db.Roles.SingleAsync(r => r.RoleName == roleName, ct);
        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            DateOfBirth = request.DateOfBirth!.Value,
            Gender = request.Gender,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
            IsActive = true
        };
        user.PasswordHash = passwords.Hash(user, request.Password);
        user.UserRoles.Add(new UserRole { User = user, RoleId = role.RoleId });

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var summary = new UserSummaryResponse(user.UserId, user.FirstName, user.LastName, user.Email, [role.RoleName]);
        return Created("/api/users/me", summary);
    }

    /// <summary>Authenticate with email and password and start a new session.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var email = NormaliseEmail(request.Email);
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.Email == email, ct);

        if (user is null || !user.IsActive || !passwords.Verify(user, request.Password))
        {
            return InvalidCredentials();
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var refreshToken = tokens.CreateRefreshToken();
        var session = new UserSession
        {
            SessionId = Guid.NewGuid(),
            UserId = user.UserId,
            RefreshTokenHash = tokens.HashRefreshToken(refreshToken),
            CreatedAt = now,
            LastRefreshedAt = now,
            ExpiresAt = now.AddDays(jwtOptions.Value.RefreshTokenDays)
        };
        db.UserSessions.Add(session);
        await db.SaveChangesAsync(ct);

        return Ok(BuildTokenResponse(user, session, refreshToken));
    }

    /// <summary>Exchange a refresh token for a new access token. The refresh token is rotated.</summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var session = await db.UserSessions
            .Include(s => s.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(s => s.RefreshTokenHash == hash, ct);

        var now = clock.GetUtcNow().UtcDateTime;
        if (session is null || !session.IsActive(now) || !session.User.IsActive)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid refresh token",
                detail: "The refresh token is invalid, expired or has been revoked. Please log in again.");
        }

        var refreshToken = tokens.CreateRefreshToken();
        session.RefreshTokenHash = tokens.HashRefreshToken(refreshToken);
        session.LastRefreshedAt = now;
        session.ExpiresAt = now.AddDays(jwtOptions.Value.RefreshTokenDays);
        await db.SaveChangesAsync(ct);

        return Ok(BuildTokenResponse(session.User, session, refreshToken));
    }

    /// <summary>End the current session. Its access and refresh tokens stop working immediately.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var sessionId = User.GetSessionId();
        var session = await db.UserSessions.SingleOrDefaultAsync(s => s.SessionId == sessionId, ct);
        if (session is not null && session.RevokedAt is null)
        {
            session.RevokedAt = clock.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync(ct);
        }

        return NoContent();
    }

    private TokenResponse BuildTokenResponse(User user, UserSession session, string refreshToken)
    {
        var roles = user.UserRoles.Select(ur => ur.Role.RoleName).OrderBy(r => r).ToList();
        var access = tokens.CreateAccessToken(user, roles, session.SessionId);
        return new TokenResponse(access.Token, refreshToken, access.ExpiresInSeconds, "Bearer",
            new UserSummaryResponse(user.UserId, user.FirstName, user.LastName, user.Email, roles));
    }

    private ObjectResult InvalidCredentials() =>
        Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials",
            detail: "The email address or password is incorrect.");

    internal static string NormaliseEmail(string email) => email.Trim().ToLowerInvariant();

    private static string? NormaliseSelfRegisterRole(string? role) => role?.Trim().ToLowerInvariant() switch
    {
        "organizer" or "organiser" => RoleNames.Organizer,
        "participant" => RoleNames.Participant,
        _ => null
    };
}
