using System.Security.Claims;
using FacetIQ.Contracts.Standings;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;
using FacetIQ.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FacetIQ.API.Controllers;

[ApiController]
[Route("[controller]")]
public class StandingController : ControllerBase
{
    private readonly IStandingRepository _standings;
    private readonly ISubjectRepository _subjects;
    private readonly IUserDirectory _users;
    private readonly TimeProvider _clock;

    public StandingController(
        IStandingRepository standings,
        ISubjectRepository subjects,
        IUserDirectory users,
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

        // Sequential: the lookups share one DbContext, which cannot run queries concurrently.
        var issuedResponses = new List<StandingResponse>();
        foreach (var standing in issued)
        {
            issuedResponses.Add(await ToContract(standing, withHolder: true, cancellationToken));
        }

        var heldResponses = new List<StandingResponse>();
        foreach (var standing in held)
        {
            heldResponses.Add(await ToContract(standing, withHolder: false, cancellationToken));
        }

        return Ok(new StandingsResponse { Issued = issuedResponses, Held = heldResponses });
    }

    /// <summary>
    /// Issues a standing about another account. It has no effect until they accept. Unlike
    /// /disclosure, an unknown email is reported, as /register already reveals it.
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

        // Resolved here because the domain does not reference the identity store.
        var holderId = await _users.FindUserIdByEmailAsync(dto.Email, cancellationToken);

        if (holderId is null)
        {
            ModelState.AddModelError(nameof(dto.Email), "No account uses that address.");

            return ValidationProblem(
                statusCode: StatusCodes.Status400BadRequest,
                modelStateDictionary: ModelState);
        }

        if (holderId == userId)
        {
            ModelState.AddModelError(nameof(dto.Email), "A standing describes someone else.");

            return ValidationProblem(
                statusCode: StatusCodes.Status400BadRequest,
                modelStateDictionary: ModelState);
        }

        // Case-insensitive, as matching is.
        var issued = await _standings.ListIssuedBySubjectAsync(subject.Id, cancellationToken);

        if (issued.Any(existing =>
                existing.RequesterUserId == holderId &&
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
            RequesterUserId = holderId,
            Value = dto.Value,
            IssuerKind = IssuerKind.Subject,
            Issuer = userId,
            IssuedAt = _clock.GetUtcNow(),
            AcceptedAt = null,
        };

        await _standings.AddAsync(standing, cancellationToken);

        return Ok(await ToContract(standing, withHolder: true, cancellationToken));
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

        // 404 rather than 403 so another holder's standing is not confirmed to exist.
        if (standing is null || standing.RequesterUserId != userId)
        {
            return NotFound();
        }

        await _standings.AcceptAsync(id, _clock.GetUtcNow(), cancellationToken);

        return NoContent();
    }

    private async Task<StandingResponse> ToContract(Standing standing, bool withHolder, CancellationToken cancellationToken)
    {
        return new StandingResponse
        {
            Id = standing.Id,
            Value = standing.Value,
            IssuerKind = standing.IssuerKind.ToString(),
            Issuer = standing.IssuerKind == IssuerKind.Subject
                ? await AddressOf(standing.Issuer, cancellationToken)
                : standing.Issuer,
            Holder = withHolder ? await AddressOf(standing.RequesterUserId, cancellationToken) : null,
            IssuedAt = standing.IssuedAt,
            AcceptedAt = standing.AcceptedAt,
        };
    }

    private async Task<string> AddressOf(string userId, CancellationToken cancellationToken)
    {
        var userEmail = await _users.FindEmailAsync(userId, cancellationToken);

        return userEmail ?? userId;
    }
}
