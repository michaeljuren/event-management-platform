using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authorization;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Athletics and cycling clubs.</summary>
[Route("api/clubs")]
public class ClubsController(RaceDayDbContext db) : ApiControllerBase
{
    /// <summary>List clubs, optionally filtered by province.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PagedResult<ClubResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] ClubQuery query, CancellationToken ct)
    {
        var clubs = db.Clubs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Province))
        {
            clubs = clubs.Where(c => c.Province == query.Province);
        }

        return Ok(await clubs.OrderBy(c => c.ClubName).ToPagedResultAsync(query, ClubResponse.From, ct));
    }

    /// <summary>Get a club's detail.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType<ClubResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var club = await db.Clubs.AsNoTracking().SingleOrDefaultAsync(c => c.ClubId == id, ct);
        return club is null ? NotFoundProblem($"Club {id} not found.") : Ok(ClubResponse.From(club));
    }

    /// <summary>Create a club.</summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType<ClubResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(ClubRequest request, CancellationToken ct)
    {
        if (!Province.IsValid(request.Province))
        {
            return BadRequestProblem(nameof(request.Province), "Province must be one of the nine South African provinces.");
        }

        var name = request.ClubName.Trim();
        if (await db.Clubs.AnyAsync(c => c.ClubName == name, ct))
        {
            return ConflictProblem($"A club named '{name}' already exists.");
        }

        var club = new Club();
        Apply(club, request);
        db.Clubs.Add(club);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = club.ClubId }, ClubResponse.From(club));
    }

    /// <summary>Update a club.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType<ClubResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, ClubRequest request, CancellationToken ct)
    {
        if (!Province.IsValid(request.Province))
        {
            return BadRequestProblem(nameof(request.Province), "Province must be one of the nine South African provinces.");
        }

        var club = await db.Clubs.SingleOrDefaultAsync(c => c.ClubId == id, ct);
        if (club is null)
        {
            return NotFoundProblem($"Club {id} not found.");
        }

        var name = request.ClubName.Trim();
        if (await db.Clubs.AnyAsync(c => c.ClubName == name && c.ClubId != id, ct))
        {
            return ConflictProblem($"A club named '{name}' already exists.");
        }

        Apply(club, request);
        await db.SaveChangesAsync(ct);
        return Ok(ClubResponse.From(club));
    }

    /// <summary>Delete a club. Members are kept and simply no longer belong to a club.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var club = await db.Clubs.Include(c => c.Members).SingleOrDefaultAsync(c => c.ClubId == id, ct);
        if (club is null)
        {
            return NotFoundProblem($"Club {id} not found.");
        }

        // Mirrors FK_Users_Clubs ON DELETE SET NULL (done explicitly so the in-memory provider behaves the same).
        foreach (var member in club.Members)
        {
            member.ClubId = null;
        }

        db.Clubs.Remove(club);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static void Apply(Club club, ClubRequest request)
    {
        club.ClubName = request.ClubName.Trim();
        club.Province = request.Province;
        club.AffiliationNumber = string.IsNullOrWhiteSpace(request.AffiliationNumber) ? null : request.AffiliationNumber.Trim();
        club.ContactEmail = string.IsNullOrWhiteSpace(request.ContactEmail) ? null : request.ContactEmail.Trim();
    }
}
