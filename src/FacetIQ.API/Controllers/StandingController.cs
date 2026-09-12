using System.Security.Claims;
using FacetIQ.Contracts.Standings;
using FacetIQ.Data.Identity;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FacetIQ.API.Controllers;

[ApiController]
[Route("[controller]")]
public class StandingController : ControllerBase
{
    private readonly IStandingRepository _standings;
    private readonly ISubjectRepository _subjects;
    private readonly UserManager<AppUser> _users;
    private readonly TimeProvider _clock;

    public StandingController(
        IStandingRepository standings,
        ISubjectRepository subjects,
        UserManager<AppUser> users,
        TimeProvider clock)
    {
        _standings = standings;
        _subjects = subjects;
        _users = users;
        _clock = clock;
    }

    /// <summary>What the caller has issued about others, and what others have issued about them.</summary>
    [HttpGet]
    public async Task<ActionResult<StandingsResponse>> Get(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return Unauthorized();
        }

        var subject = await _subjects.FindByUserIdAsync(userId, cancellationToken);

        var issued = subject is null
            ? []
            : await _standings.ListIssuedBySubjectAsync(subject.Id, cancellationToken);

        var held = await _standings.ListHeldByAsync(userId, cancellationToken);

        // One at a time: every lookup uses this request's database context, which refuses a second
        // query while one is still open.
        var issuedResponses = new List<StandingResponse>();
        foreach (var standing in issued)
        {
            issuedResponses.Add(await ToContract(standing, withHolder: true));
        }

        var heldResponses = new List<StandingResponse>();
        foreach (var standing in held)
        {
            heldResponses.Add(await ToContract(standing, withHolder: false));
        }

        return Ok(new StandingsResponse { Issued = issuedResponses, Held = heldResponses });
    }

    /// <summary>
    /// Issues a standing about someone else. It is inert until they accept, which is what stops
    /// either party asserting a relationship about the other on their own.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<StandingResponse>> Post(
        IssueStandingRequest dto,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return Unauthorized();
        }

        var subject = await _subjects.FindByUserIdAsync(userId, cancellationToken);

        if (subject is null)
        {
            return Forbid();
        }

        // Resolved here rather than in the domain, which names no identity store.
        var holder = await _users.FindByEmailAsync(dto.Email);

        if (holder?.Id is null)
        {
            ModelState.AddModelError(nameof(dto.Email), "No account uses that address.");

            return ValidationProblem(
                statusCode: StatusCodes.Status400BadRequest,
                modelStateDictionary: ModelState);
        }

        if (holder.Id == userId)
        {
            ModelState.AddModelError(nameof(dto.Email), "A standing describes someone else.");

            return ValidationProblem(
                statusCode: StatusCodes.Status400BadRequest,
                modelStateDictionary: ModelState);
        }

        // The same term for the same person twice adds nothing to matching and shows twice on
        // screen. Compared as matching compares it, so a change of case is not a new standing.
        var issued = await _standings.ListIssuedBySubjectAsync(subject.Id, cancellationToken);

        if (issued.Any(existing =>
                existing.RequesterUserId == holder.Id &&
                string.Equals(existing.Value, dto.Value, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(dto.Value), "You have already added them as that.");

            return ValidationProblem(
                statusCode: StatusCodes.Status400BadRequest,
                modelStateDictionary: ModelState);
        }

        var standing = new Standing
        {
            Id = Guid.NewGuid(),
            SubjectId = subject.Id,
            RequesterUserId = holder.Id,
            Value = dto.Value,
            IssuerKind = IssuerKind.Subject,
            Issuer = userId,
            IssuedAt = _clock.GetUtcNow(),
            AcceptedAt = null,
        };

        await _standings.AddAsync(standing, cancellationToken);

        return Ok(await ToContract(standing, withHolder: true));
    }

    /// <summary>Accepts a standing issued about the caller. Only the holder can.</summary>
    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return Unauthorized();
        }

        var standing = await _standings.FindAsync(id, cancellationToken);

        // Not found rather than forbidden for someone else's standing, so the response does not
        // confirm that it exists.
        if (standing is null || standing.RequesterUserId != userId)
        {
            return NotFound();
        }

        await _standings.AcceptAsync(id, _clock.GetUtcNow(), cancellationToken);

        return NoContent();
    }

    private async Task<StandingResponse> ToContract(Standing standing, bool withHolder)
    {
        return new StandingResponse
        {
            Id = standing.Id,
            Value = standing.Value,
            IssuerKind = standing.IssuerKind.ToString(),
            Issuer = standing.IssuerKind == IssuerKind.Subject
                ? await AddressOf(standing.Issuer)
                : standing.Issuer,
            Holder = withHolder ? await AddressOf(standing.RequesterUserId) : null,
            IssuedAt = standing.IssuedAt,
            AcceptedAt = standing.AcceptedAt,
        };
    }

    private async Task<string> AddressOf(string userId)
    {
        var user = await _users.FindByIdAsync(userId);

        return user?.Email ?? userId;
    }
}
