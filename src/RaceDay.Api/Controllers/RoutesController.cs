using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authorization;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Courses at a venue that categories are run or ridden on.</summary>
/// <remarks>
/// The schema has no owner column on Routes, so "owner" is derived: an organizer may change or
/// remove a route unless it is used by a category of another organizer's event. Admins always may.
/// </remarks>
public class RoutesController(RaceDayDbContext db) : ApiControllerBase
{
    /// <summary>List the routes at a venue.</summary>
    [HttpGet("api/venues/{venueId:int}/routes")]
    [AllowAnonymous]
    [ProducesResponseType<List<RouteResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListForVenue(int venueId, CancellationToken ct)
    {
        if (!await db.Venues.AnyAsync(v => v.VenueId == venueId, ct))
        {
            return NotFoundProblem($"Venue {venueId} not found.");
        }

        var routes = await db.Routes.AsNoTracking()
            .Where(r => r.VenueId == venueId)
            .OrderBy(r => r.DistanceKm)
            .ToListAsync(ct);
        return Ok(routes.Select(RouteResponse.From).ToList());
    }

    /// <summary>Add a route to a venue.</summary>
    [HttpPost("api/venues/{venueId:int}/routes")]
    [Authorize(Roles = RoleNames.OrganizerOrAdmin)]
    [ProducesResponseType<RouteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int venueId, RouteRequest request, CancellationToken ct)
    {
        if (!await db.Venues.AnyAsync(v => v.VenueId == venueId, ct))
        {
            return NotFoundProblem($"Venue {venueId} not found.");
        }

        var route = new RaceRoute { VenueId = venueId };
        Apply(route, request);
        db.Routes.Add(route);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = route.RouteId }, RouteResponse.From(route));
    }

    /// <summary>Get a route's detail.</summary>
    [HttpGet("api/routes/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType<RouteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var route = await db.Routes.AsNoTracking().SingleOrDefaultAsync(r => r.RouteId == id, ct);
        return route is null ? NotFoundProblem($"Route {id} not found.") : Ok(RouteResponse.From(route));
    }

    /// <summary>Update a route.</summary>
    [HttpPut("api/routes/{id:int}")]
    [Authorize(Roles = RoleNames.OrganizerOrAdmin)]
    [ProducesResponseType<RouteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, RouteRequest request, CancellationToken ct)
    {
        var route = await db.Routes.SingleOrDefaultAsync(r => r.RouteId == id, ct);
        if (route is null)
        {
            return NotFoundProblem($"Route {id} not found.");
        }

        if (!await CanManageAsync(id, ct))
        {
            return ForbiddenProblem("This route is used by another organizer's event.");
        }

        Apply(route, request);
        await db.SaveChangesAsync(ct);
        return Ok(RouteResponse.From(route));
    }

    /// <summary>Delete a route. Categories that used it keep their own distance and simply lose the route link.</summary>
    [HttpDelete("api/routes/{id:int}")]
    [Authorize(Roles = RoleNames.OrganizerOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var route = await db.Routes.Include(r => r.Categories).SingleOrDefaultAsync(r => r.RouteId == id, ct);
        if (route is null)
        {
            return NotFoundProblem($"Route {id} not found.");
        }

        if (!await CanManageAsync(id, ct))
        {
            return ForbiddenProblem("This route is used by another organizer's event.");
        }

        // Mirrors FK_Categories_Routes ON DELETE SET NULL.
        foreach (var category in route.Categories)
        {
            category.RouteId = null;
        }

        db.Routes.Remove(route);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<bool> CanManageAsync(int routeId, CancellationToken ct)
    {
        if (IsAdmin)
        {
            return true;
        }

        var me = CurrentUserId;
        return !await db.Categories.AnyAsync(c => c.RouteId == routeId && c.Event.OrganizerId != me, ct);
    }

    private static void Apply(RaceRoute route, RouteRequest request)
    {
        route.RouteName = request.RouteName.Trim();
        route.DistanceKm = request.DistanceKm;
        route.ElevationGainM = request.ElevationGainM;
        route.MapUrl = string.IsNullOrWhiteSpace(request.MapUrl) ? null : request.MapUrl.Trim();
        route.StartPointDescription = string.IsNullOrWhiteSpace(request.StartPointDescription) ? null : request.StartPointDescription.Trim();
        route.EndPointDescription = string.IsNullOrWhiteSpace(request.EndPointDescription) ? null : request.EndPointDescription.Trim();
    }
}
