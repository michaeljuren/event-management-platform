using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Authorization;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Shared helpers for the current user and consistent ProblemDetails responses.</summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentUserId => User.GetUserId();

    protected bool IsAdmin => User.IsInRole(RoleNames.Admin);

    protected bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

    /// <summary>The event's own organizer, or an admin, may manage it and everything under it.</summary>
    protected bool CanManage(Event evt) => IsAuthenticated && (IsAdmin || evt.OrganizerId == CurrentUserId);

    /// <summary>Draft events are only visible to the people who can manage them.</summary>
    protected bool CanView(Event evt) => evt.Status != EventStatus.Draft || CanManage(evt);

    protected ObjectResult NotFoundProblem(string detail) =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found", detail: detail);

    protected ObjectResult ConflictProblem(string detail) =>
        Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflict", detail: detail);

    protected ObjectResult ForbiddenProblem(string detail) =>
        Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: detail);

    protected ActionResult BadRequestProblem(string field, string message)
    {
        ModelState.AddModelError(field, message);
        return ValidationProblem(ModelState);
    }
}
