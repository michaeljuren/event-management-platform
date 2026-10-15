using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authorization;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Profiles, performance history and (for admins) role management.</summary>
[Route("api/users")]
[Authorize]
public class UsersController(RaceDayDbContext db) : ApiControllerBase
{
    /// <summary>Get the signed-in user's own profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var user = await LoadUserAsync(CurrentUserId, ct);
        return user is null ? NotFoundProblem("User not found.") : Ok(UserProfileResponse.From(user));
    }

    /// <summary>Update the signed-in user's own profile.</summary>
    [HttpPut("me")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request, CancellationToken ct)
    {
        if (request.ClubId is { } clubId && !await db.Clubs.AnyAsync(c => c.ClubId == clubId, ct))
        {
            return BadRequestProblem(nameof(request.ClubId), $"Club {clubId} does not exist.");
        }

        var user = await LoadUserAsync(CurrentUserId, ct);
        if (user is null)
        {
            return NotFoundProblem("User not found.");
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = Blank(request.PhoneNumber);
        user.ClubId = request.ClubId;
        user.EmergencyContactName = Blank(request.EmergencyContactName);
        user.EmergencyContactPhone = Blank(request.EmergencyContactPhone);
        await db.SaveChangesAsync(ct);

        await db.Entry(user).Reference(u => u.Club).LoadAsync(ct);
        return Ok(UserProfileResponse.From(user));
    }

    /// <summary>Personal performance history: every result recorded against the user's entries.</summary>
    [HttpGet("me/performance-history")]
    [Authorize(Roles = RoleNames.Participant)]
    [ProducesResponseType<List<PerformanceHistoryItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPerformanceHistory(CancellationToken ct)
    {
        var userId = CurrentUserId;
        var history = await db.Results
            .AsNoTracking()
            .Where(r => r.Entry.ParticipantId == userId)
            .OrderByDescending(r => r.Entry.Category.Event.EventDate)
            .Select(r => new PerformanceHistoryItem(
                r.EntryId,
                r.Entry.Category.EventId,
                r.Entry.Category.Event.EventName,
                r.Entry.Category.Event.EventDate,
                r.Entry.Category.Event.Discipline,
                r.Entry.CategoryId,
                r.Entry.Category.CategoryName,
                r.Entry.Category.DistanceKm,
                r.FinishTime,
                r.OverallPosition,
                r.CategoryPosition,
                r.GenderPosition,
                r.FinishStatus))
            .ToListAsync(ct);

        return Ok(history);
    }

    /// <summary>Get any user's profile by id.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var user = await LoadUserAsync(id, ct);
        return user is null ? NotFoundProblem($"User {id} not found.") : Ok(UserProfileResponse.From(user));
    }

    /// <summary>Grant a role to a user. Returns the user's updated role list.</summary>
    [HttpPost("{id:int}/roles")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType<List<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GrantRole(int id, GrantRoleRequest request, CancellationToken ct)
    {
        var role = await db.Roles.SingleOrDefaultAsync(r => r.RoleName == request.RoleName.Trim(), ct);
        if (role is null)
        {
            return BadRequestProblem(nameof(request.RoleName), $"Role must be one of: {string.Join(", ", RoleNames.All)}.");
        }

        var user = await LoadUserAsync(id, ct);
        if (user is null)
        {
            return NotFoundProblem($"User {id} not found.");
        }

        if (user.UserRoles.All(ur => ur.RoleId != role.RoleId))
        {
            user.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = role.RoleId, Role = role });
            await db.SaveChangesAsync(ct);
        }

        return Ok(user.UserRoles.Select(ur => ur.Role.RoleName).OrderBy(r => r).ToList());
    }

    private Task<User?> LoadUserAsync(int id, CancellationToken ct) =>
        db.Users
            .Include(u => u.Club)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.UserId == id, ct);

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
