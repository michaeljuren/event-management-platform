using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authorization;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Participants entering events, and organizers managing their rosters.</summary>
public class EntriesController(RaceDayDbContext db, TimeProvider clock) : ApiControllerBase
{
    /// <summary>Enter an event by selecting one of its categories.</summary>
    /// <remarks>
    /// The entry records the participant, the category and (through the category) the event.
    /// Rejected when the event is not open for registration (400), the participant does not meet the
    /// category's age or gender rules (400), the participant has already entered this event (409)
    /// or the category is full (409).
    /// </remarks>
    [HttpPost("api/categories/{categoryId:int}/entries")]
    [Authorize(Roles = RoleNames.Participant)]
    [ProducesResponseType<EntryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enter(int categoryId, CancellationToken ct)
    {
        var category = await db.Categories
            .Include(c => c.Event)
            .SingleOrDefaultAsync(c => c.CategoryId == categoryId, ct);
        if (category is null || category.Event.Status == EventStatus.Draft)
        {
            return NotFoundProblem($"Category {categoryId} not found.");
        }

        var evt = category.Event;
        var now = clock.GetUtcNow().UtcDateTime;
        if (evt.Status != EventStatus.Published)
        {
            return BadRequestProblem("Event", $"This event is {evt.Status} and no longer accepts entries.");
        }

        if (now < evt.RegistrationOpenDate || now > evt.RegistrationCloseDate)
        {
            return BadRequestProblem("Event", "Registration for this event is not open.");
        }

        var participantId = CurrentUserId;
        if (evt.OrganizerId == participantId)
        {
            return BadRequestProblem("Event", "Organizers cannot enter their own event.");
        }

        var participant = await db.Users.SingleAsync(u => u.UserId == participantId, ct);
        if (category.MinAge is { } minAge && AgeOn(participant.DateOfBirth, evt.EventDate) < minAge)
        {
            return BadRequestProblem("Category", $"Participants in this category must be at least {minAge} on race day.");
        }

        if (category.GenderRestriction != Gender.Open && category.GenderRestriction != participant.Gender)
        {
            return BadRequestProblem("Category", $"This category is restricted to {category.GenderRestriction} participants.");
        }

        var alreadyEntered = await db.Entries.AnyAsync(e =>
            e.ParticipantId == participantId &&
            (e.CategoryId == categoryId ||
             (e.Category.EventId == evt.EventId && EntryStatus.Active.Contains(e.Status))), ct);
        if (alreadyEntered)
        {
            return ConflictProblem("You have already entered this event.");
        }

        if (category.MaxParticipants is { } max &&
            await db.Entries.CountAsync(e => e.CategoryId == categoryId && EntryStatus.Active.Contains(e.Status), ct) >= max)
        {
            return ConflictProblem("This category is full.");
        }

        var entry = new Entry
        {
            CategoryId = categoryId,
            ParticipantId = participantId,
            EntryDate = now,
            Status = category.EntryFee == 0 ? EntryStatus.Confirmed : EntryStatus.Pending,
            AmountPaid = 0,
            PaymentStatus = PaymentStatus.Unpaid
        };
        db.Entries.Add(entry);
        await db.SaveChangesAsync(ct);

        var created = await LoadEntryAsync(entry.EntryId, ct);
        return CreatedAtAction(nameof(Get), new { id = entry.EntryId }, EntryResponse.From(created!));
    }

    /// <summary>List the signed-in participant's own entries.</summary>
    [HttpGet("api/users/me/entries")]
    [Authorize(Roles = RoleNames.Participant)]
    [ProducesResponseType<List<EntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MyEntries(CancellationToken ct)
    {
        var userId = CurrentUserId;
        var entries = await EntriesWithDetails()
            .Where(e => e.ParticipantId == userId)
            .OrderByDescending(e => e.Category.Event.EventDate)
            .ToListAsync(ct);
        return Ok(entries.Select(EntryResponse.From).ToList());
    }

    /// <summary>List all entries for an event (the organizer's roster).</summary>
    [HttpGet("api/events/{eventId:int}/entries")]
    [Authorize(Roles = RoleNames.OrganizerOrAdmin)]
    [ProducesResponseType<PagedResult<RosterEntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EventRoster(int eventId, [FromQuery] PageQuery paging, CancellationToken ct)
    {
        var evt = await db.Events.AsNoTracking().SingleOrDefaultAsync(e => e.EventId == eventId, ct);
        if (evt is null)
        {
            return NotFoundProblem($"Event {eventId} not found.");
        }

        if (!CanManage(evt))
        {
            return ForbiddenProblem("Only the event's organizer or an admin can view its entries.");
        }

        var roster = await db.Entries.AsNoTracking()
            .Include(e => e.Category)
            .Include(e => e.Participant).ThenInclude(p => p.Club)
            .Where(e => e.Category.EventId == eventId)
            .OrderBy(e => e.Category.CategoryName).ThenBy(e => e.Participant.LastName).ThenBy(e => e.Participant.FirstName)
            .ToPagedResultAsync(paging, e => new RosterEntryResponse(
                e.EntryId, e.CategoryId, e.Category.CategoryName, e.ParticipantId,
                e.Participant.FirstName, e.Participant.LastName, e.Participant.Email, e.Participant.Gender,
                e.Participant.DateOfBirth, e.Participant.Club?.ClubName,
                e.Participant.EmergencyContactName, e.Participant.EmergencyContactPhone,
                e.BibNumber, e.EntryDate, e.Status, e.PaymentStatus), ct);
        return Ok(roster);
    }

    /// <summary>Get a single entry. Visible to the participant who made it, the event's organizer and admins.</summary>
    [HttpGet("api/entries/{id:int}")]
    [Authorize]
    [ProducesResponseType<EntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var entry = await LoadEntryAsync(id, ct);
        if (entry is null)
        {
            return NotFoundProblem($"Entry {id} not found.");
        }

        if (entry.ParticipantId != CurrentUserId && !CanManage(entry.Category.Event))
        {
            return ForbiddenProblem("You can only view your own entries or entries for events you organise.");
        }

        return Ok(EntryResponse.From(entry));
    }

    /// <summary>Cancel (withdraw) your own entry.</summary>
    [HttpPut("api/entries/{id:int}/cancel")]
    [Authorize(Roles = RoleNames.Participant)]
    [ProducesResponseType<EntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var entry = await LoadEntryAsync(id, ct, tracking: true);
        if (entry is null)
        {
            return NotFoundProblem($"Entry {id} not found.");
        }

        if (entry.ParticipantId != CurrentUserId)
        {
            return ForbiddenProblem("You can only cancel your own entries.");
        }

        if (!EntryStatus.Active.Contains(entry.Status))
        {
            return ConflictProblem($"This entry is already {entry.Status}.");
        }

        if (entry.Result is not null || entry.Category.Event.EventDate < DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime))
        {
            return ConflictProblem("This event has already taken place.");
        }

        entry.Status = EntryStatus.Withdrawn;
        await db.SaveChangesAsync(ct);
        return Ok(EntryResponse.From(entry));
    }

    /// <summary>Assign a bib number to an entry. Bib numbers are unique within an event.</summary>
    [HttpPut("api/entries/{id:int}/bib")]
    [Authorize(Roles = RoleNames.OrganizerOrAdmin)]
    [ProducesResponseType<EntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignBib(int id, AssignBibRequest request, CancellationToken ct)
    {
        var entry = await LoadEntryAsync(id, ct, tracking: true);
        if (entry is null)
        {
            return NotFoundProblem($"Entry {id} not found.");
        }

        if (!CanManage(entry.Category.Event))
        {
            return ForbiddenProblem("Only the event's organizer or an admin can assign bib numbers.");
        }

        var bib = request.BibNumber.Trim();
        var eventId = entry.Category.EventId;
        if (await db.Entries.AnyAsync(e => e.Category.EventId == eventId && e.BibNumber == bib && e.EntryId != id, ct))
        {
            return BadRequestProblem(nameof(request.BibNumber), $"Bib {bib} is already assigned to another entry in this event.");
        }

        entry.BibNumber = bib;
        await db.SaveChangesAsync(ct);
        return Ok(EntryResponse.From(entry));
    }

    private IQueryable<Entry> EntriesWithDetails() => db.Entries
        .Include(e => e.Category).ThenInclude(c => c.Event)
        .Include(e => e.Participant)
        .Include(e => e.Result);

    private Task<Entry?> LoadEntryAsync(int id, CancellationToken ct, bool tracking = false)
    {
        var query = EntriesWithDetails();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(e => e.EntryId == id, ct);
    }

    private static int AgeOn(DateOnly dateOfBirth, DateOnly onDate)
    {
        var age = onDate.Year - dateOfBirth.Year;
        if (dateOfBirth > onDate.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}
