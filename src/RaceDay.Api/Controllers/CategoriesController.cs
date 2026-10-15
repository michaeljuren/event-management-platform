using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authorization;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Age and distance categories within an event (e.g. Under 20, Senior, 10km, 21km).</summary>
public class CategoriesController(RaceDayDbContext db) : ApiControllerBase
{
    /// <summary>List the categories for an event.</summary>
    [HttpGet("api/events/{eventId:int}/categories")]
    [AllowAnonymous]
    [ProducesResponseType<List<CategoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListForEvent(int eventId, CancellationToken ct)
    {
        var evt = await db.Events.AsNoTracking()
            .Include(e => e.Categories).ThenInclude(c => c.Entries)
            .SingleOrDefaultAsync(e => e.EventId == eventId, ct);
        if (evt is null || !CanView(evt))
        {
            return NotFoundProblem($"Event {eventId} not found.");
        }

        return Ok(evt.Categories
            .OrderBy(c => c.DistanceKm).ThenBy(c => c.CategoryName)
            .Select(CategoryResponse.From)
            .ToList());
    }

    /// <summary>Add a category to an event.</summary>
    [HttpPost("api/events/{eventId:int}/categories")]
    [Authorize(Roles = RoleNames.OrganizerOrAdmin)]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int eventId, CategoryRequest request, CancellationToken ct)
    {
        var evt = await db.Events.SingleOrDefaultAsync(e => e.EventId == eventId, ct);
        if (evt is null)
        {
            return NotFoundProblem($"Event {eventId} not found.");
        }

        if (!CanManage(evt))
        {
            return ForbiddenProblem("Only the event's organizer or an admin can add categories.");
        }

        await ValidateAsync(evt, request, ct);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var name = request.CategoryName.Trim();
        if (await db.Categories.AnyAsync(c => c.EventId == eventId && c.CategoryName == name, ct))
        {
            return ConflictProblem($"This event already has a category named '{name}'.");
        }

        var category = new Category { EventId = eventId };
        Apply(category, request);
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = category.CategoryId }, CategoryResponse.From(category));
    }

    /// <summary>Get a category's detail.</summary>
    [HttpGet("api/categories/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var category = await db.Categories.AsNoTracking()
            .Include(c => c.Event)
            .Include(c => c.Entries)
            .SingleOrDefaultAsync(c => c.CategoryId == id, ct);
        return category is null || !CanView(category.Event)
            ? NotFoundProblem($"Category {id} not found.")
            : Ok(CategoryResponse.From(category));
    }

    /// <summary>Update a category.</summary>
    [HttpPut("api/categories/{id:int}")]
    [Authorize(Roles = RoleNames.OrganizerOrAdmin)]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, CategoryRequest request, CancellationToken ct)
    {
        var category = await db.Categories
            .Include(c => c.Event)
            .Include(c => c.Entries)
            .SingleOrDefaultAsync(c => c.CategoryId == id, ct);
        if (category is null)
        {
            return NotFoundProblem($"Category {id} not found.");
        }

        if (!CanManage(category.Event))
        {
            return ForbiddenProblem("Only the event's organizer or an admin can update its categories.");
        }

        await ValidateAsync(category.Event, request, ct);
        var activeEntries = category.Entries.Count(e => EntryStatus.Active.Contains(e.Status));
        if (request.MaxParticipants is { } max && max < activeEntries)
        {
            ModelState.AddModelError(nameof(request.MaxParticipants), $"There are already {activeEntries} active entries in this category.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var name = request.CategoryName.Trim();
        if (await db.Categories.AnyAsync(c => c.EventId == category.EventId && c.CategoryName == name && c.CategoryId != id, ct))
        {
            return ConflictProblem($"This event already has a category named '{name}'.");
        }

        Apply(category, request);
        await db.SaveChangesAsync(ct);
        return Ok(CategoryResponse.From(category));
    }

    /// <summary>Remove a category. Rejected with 409 if anyone has entered it.</summary>
    [HttpDelete("api/categories/{id:int}")]
    [Authorize(Roles = RoleNames.OrganizerOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var category = await db.Categories
            .Include(c => c.Event)
            .SingleOrDefaultAsync(c => c.CategoryId == id, ct);
        if (category is null)
        {
            return NotFoundProblem($"Category {id} not found.");
        }

        if (!CanManage(category.Event))
        {
            return ForbiddenProblem("Only the event's organizer or an admin can remove its categories.");
        }

        if (await db.Entries.AnyAsync(e => e.CategoryId == id, ct))
        {
            return ConflictProblem("Participants have entered this category, so it cannot be removed.");
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task ValidateAsync(Event evt, CategoryRequest request, CancellationToken ct)
    {
        if (request.RouteId is { } routeId &&
            !await db.Routes.AnyAsync(r => r.RouteId == routeId && r.VenueId == evt.VenueId, ct))
        {
            ModelState.AddModelError(nameof(request.RouteId), $"Route {routeId} does not exist at this event's venue.");
        }

        if (request.GenderRestriction is { } gender && !Gender.RestrictionValues.Contains(gender))
        {
            ModelState.AddModelError(nameof(request.GenderRestriction), "Gender restriction must be Open, Male or Female.");
        }
    }

    private static void Apply(Category category, CategoryRequest request)
    {
        category.RouteId = request.RouteId;
        category.CategoryName = request.CategoryName.Trim();
        category.DistanceKm = request.DistanceKm;
        category.MinAge = request.MinAge;
        category.GenderRestriction = request.GenderRestriction ?? Gender.Open;
        category.EntryFee = request.EntryFee;
        category.MaxParticipants = request.MaxParticipants;
    }
}
