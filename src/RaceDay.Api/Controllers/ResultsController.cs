using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authorization;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Finish times and positions captured by organizers after an event.</summary>
public class ResultsController(RaceDayDbContext db, TimeProvider clock) : ApiControllerBase
{
    /// <summary>Capture the result for an entry once the event has taken place.</summary>
    [HttpPost("api/entries/{entryId:int}/result")]
    [Authorize(Roles = RoleNames.OrganizerOrAdmin)]
    [ProducesResponseType<ResultResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int entryId, ResultRequest request, CancellationToken ct)
    {
        var entry = await db.Entries
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Participant)
            .Include(e => e.Result)
            .SingleOrDefaultAsync(e => e.EntryId == entryId, ct);
        if (entry is null)
        {
            return NotFoundProblem($"Entry {entryId} not found.");
        }

        if (!CanManage(entry.Category.Event))
        {
            return ForbiddenProblem("Only the event's organizer or an admin can capture results.");
        }

        if (entry.Result is not null)
        {
            return ConflictProblem("A result has already been captured for this entry. Use PUT /api/results/{id} to correct it.");
        }

        if (!EntryStatus.Active.Contains(entry.Status))
        {
            ModelState.AddModelError("Entry", $"Cannot capture a result for a {entry.Status} entry.");
        }

        if (entry.Category.Event.EventDate > DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime))
        {
            ModelState.AddModelError("Event", "Results can only be captured after the event has taken place.");
        }

        var finishStatus = Validate(request);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = new Result
        {
            EntryId = entryId,
            Entry = entry,
            RecordedByUserId = CurrentUserId,
            RecordedAt = clock.GetUtcNow().UtcDateTime
        };
        Apply(result, request, finishStatus!);
        db.Results.Add(result);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = result.ResultId }, ResultResponse.From(result));
    }

    /// <summary>Full results (leaderboard) for an event.</summary>
    [HttpGet("api/events/{eventId:int}/results")]
    [AllowAnonymous]
    [ProducesResponseType<PagedResult<ResultResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForEvent(int eventId, [FromQuery] PageQuery paging, CancellationToken ct)
    {
        var evt = await db.Events.AsNoTracking().SingleOrDefaultAsync(e => e.EventId == eventId, ct);
        if (evt is null || !CanView(evt))
        {
            return NotFoundProblem($"Event {eventId} not found.");
        }

        var page = await Leaderboard(ResultsWithDetails().Where(r => r.Entry.Category.EventId == eventId))
            .ToPagedResultAsync(paging, ResultResponse.From, ct);
        return Ok(page);
    }

    /// <summary>Results for a single category.</summary>
    [HttpGet("api/categories/{categoryId:int}/results")]
    [AllowAnonymous]
    [ProducesResponseType<List<ResultResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForCategory(int categoryId, CancellationToken ct)
    {
        var category = await db.Categories.AsNoTracking()
            .Include(c => c.Event)
            .SingleOrDefaultAsync(c => c.CategoryId == categoryId, ct);
        if (category is null || !CanView(category.Event))
        {
            return NotFoundProblem($"Category {categoryId} not found.");
        }

        var results = await Leaderboard(ResultsWithDetails().Where(r => r.Entry.CategoryId == categoryId)).ToListAsync(ct);
        return Ok(results.Select(ResultResponse.From).ToList());
    }

    /// <summary>Get a single result.</summary>
    [HttpGet("api/results/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType<ResultResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var result = await ResultsWithDetails().AsNoTracking().SingleOrDefaultAsync(r => r.ResultId == id, ct);
        return result is null || !CanView(result.Entry.Category.Event)
            ? NotFoundProblem($"Result {id} not found.")
            : Ok(ResultResponse.From(result));
    }

    /// <summary>Correct or update a result.</summary>
    [HttpPut("api/results/{id:int}")]
    [Authorize(Roles = RoleNames.OrganizerOrAdmin)]
    [ProducesResponseType<ResultResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, ResultRequest request, CancellationToken ct)
    {
        var result = await ResultsWithDetails().SingleOrDefaultAsync(r => r.ResultId == id, ct);
        if (result is null)
        {
            return NotFoundProblem($"Result {id} not found.");
        }

        if (!CanManage(result.Entry.Category.Event))
        {
            return ForbiddenProblem("Only the event's organizer or an admin can update results.");
        }

        var finishStatus = Validate(request);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        Apply(result, request, finishStatus!);
        result.RecordedByUserId = CurrentUserId;
        result.RecordedAt = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        return Ok(ResultResponse.From(result));
    }

    /// <summary>Remove a result.</summary>
    [HttpDelete("api/results/{id:int}")]
    [Authorize(Roles = RoleNames.OrganizerOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await db.Results
            .Include(r => r.Entry).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
            .SingleOrDefaultAsync(r => r.ResultId == id, ct);
        if (result is null)
        {
            return NotFoundProblem($"Result {id} not found.");
        }

        if (!CanManage(result.Entry.Category.Event))
        {
            return ForbiddenProblem("Only the event's organizer or an admin can remove results.");
        }

        db.Results.Remove(result);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private IQueryable<Result> ResultsWithDetails() => db.Results
        .Include(r => r.Entry).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
        .Include(r => r.Entry).ThenInclude(e => e.Participant);

    /// <summary>Placed finishers first (by position, then time), then unplaced finishers, then DNF/DNS/DSQ.</summary>
    private static IOrderedQueryable<Result> Leaderboard(IQueryable<Result> results) => results
        .OrderBy(r => r.FinishStatus == FinishStatus.Finished ? 0 : 1)
        .ThenBy(r => r.OverallPosition == null ? 1 : 0)
        .ThenBy(r => r.OverallPosition)
        .ThenBy(r => r.FinishTime);

    /// <summary>Validates the request and returns the normalised finish status.</summary>
    private string? Validate(ResultRequest request)
    {
        var status = request.FinishStatus is null
            ? FinishStatus.Finished
            : FinishStatus.All.FirstOrDefault(s => string.Equals(s, request.FinishStatus.Trim(), StringComparison.OrdinalIgnoreCase));
        if (status is null)
        {
            ModelState.AddModelError(nameof(request.FinishStatus), "Finish status must be Finished, DNF, DNS or DSQ.");
            return null;
        }

        if (status == FinishStatus.Finished && request.FinishTime is null)
        {
            ModelState.AddModelError(nameof(request.FinishTime), "A finish time is required for a Finished result.");
        }

        if (status != FinishStatus.Finished &&
            (request.OverallPosition is not null || request.CategoryPosition is not null || request.GenderPosition is not null))
        {
            ModelState.AddModelError(nameof(request.OverallPosition), $"A {status} result cannot have finishing positions.");
        }

        return status;
    }

    private static void Apply(Result result, ResultRequest request, string finishStatus)
    {
        result.FinishTime = request.FinishTime;
        result.GunTime = request.GunTime;
        result.ChipTime = request.ChipTime;
        result.OverallPosition = request.OverallPosition;
        result.CategoryPosition = request.CategoryPosition;
        result.GenderPosition = request.GenderPosition;
        result.FinishStatus = finishStatus;
    }
}
