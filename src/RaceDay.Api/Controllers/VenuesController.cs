using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authorization;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Physical locations that events are held at.</summary>
[Route("api/venues")]
public class VenuesController(RaceDayDbContext db) : ApiControllerBase
{
    /// <summary>List venues.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PagedResult<VenueResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] PageQuery paging, CancellationToken ct) =>
        Ok(await db.Venues.AsNoTracking().OrderBy(v => v.VenueName).ToPagedResultAsync(paging, VenueResponse.From, ct));

    /// <summary>Get a venue's detail.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType<VenueResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var venue = await db.Venues.AsNoTracking().SingleOrDefaultAsync(v => v.VenueId == id, ct);
        return venue is null ? NotFoundProblem($"Venue {id} not found.") : Ok(VenueResponse.From(venue));
    }

    /// <summary>Create a venue.</summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.OrganizerOrAdmin)]
    [ProducesResponseType<VenueResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(VenueRequest request, CancellationToken ct)
    {
        if (!Province.IsValid(request.Province))
        {
            return BadRequestProblem(nameof(request.Province), "Province must be one of the nine South African provinces.");
        }

        var venue = new Venue();
        Apply(venue, request);
        db.Venues.Add(venue);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = venue.VenueId }, VenueResponse.From(venue));
    }

    /// <summary>Update a venue.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.OrganizerOrAdmin)]
    [ProducesResponseType<VenueResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, VenueRequest request, CancellationToken ct)
    {
        if (!Province.IsValid(request.Province))
        {
            return BadRequestProblem(nameof(request.Province), "Province must be one of the nine South African provinces.");
        }

        var venue = await db.Venues.SingleOrDefaultAsync(v => v.VenueId == id, ct);
        if (venue is null)
        {
            return NotFoundProblem($"Venue {id} not found.");
        }

        Apply(venue, request);
        await db.SaveChangesAsync(ct);
        return Ok(VenueResponse.From(venue));
    }

    /// <summary>Delete a venue and its routes. Rejected with 409 if any event is held at it.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var venue = await db.Venues
            .Include(v => v.Routes).ThenInclude(r => r.Categories)
            .SingleOrDefaultAsync(v => v.VenueId == id, ct);
        if (venue is null)
        {
            return NotFoundProblem($"Venue {id} not found.");
        }

        if (await db.Events.AnyAsync(e => e.VenueId == id, ct))
        {
            return ConflictProblem("This venue is used by one or more events and cannot be deleted.");
        }

        foreach (var route in venue.Routes)
        {
            foreach (var category in route.Categories)
            {
                category.RouteId = null;
            }
        }

        db.Routes.RemoveRange(venue.Routes);
        db.Venues.Remove(venue);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static void Apply(Venue venue, VenueRequest request)
    {
        venue.VenueName = request.VenueName.Trim();
        venue.AddressLine = string.IsNullOrWhiteSpace(request.AddressLine) ? null : request.AddressLine.Trim();
        venue.City = request.City.Trim();
        venue.Province = request.Province;
        venue.PostalCode = string.IsNullOrWhiteSpace(request.PostalCode) ? null : request.PostalCode.Trim();
        venue.Latitude = request.Latitude;
        venue.Longitude = request.Longitude;
    }
}
