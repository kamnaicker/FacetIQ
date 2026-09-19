using System.Security.Claims;
using FacetIQ.Contracts.Subjects;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FacetIQ.API.Controllers;

[ApiController]
[Route("[controller]")]
public class SubjectController : ControllerBase
{
    private readonly ISubjectRepository _subjects;

    public SubjectController(ISubjectRepository subjects) => _subjects = subjects;

    /// <summary>The caller's own profile, or 404 when they hold none.</summary>
    [HttpGet]
    public async Task<ActionResult<SubjectResponse>> Get(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return Unauthorized();
        }

        var subject = await _subjects.FindByUserIdAsync(userId, cancellationToken);

        return subject is null ? NotFound() : Ok(new SubjectResponse { Id = subject.Id });
    }

    /// <summary>Creates the caller's profile, or returns the existing one. Safe to call on every sign-in.</summary>
    [HttpPost]
    public async Task<ActionResult<SubjectResponse>> Post(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return Unauthorized();
        }

        var existing = await _subjects.FindByUserIdAsync(userId, cancellationToken);

        if (existing is not null)
        {
            return Ok(new SubjectResponse { Id = existing.Id });
        }

        var subject = new Subject { Id = Guid.NewGuid(), UserId = userId };

        await _subjects.AddAsync(subject, cancellationToken);

        return Ok(new SubjectResponse { Id = subject.Id });
    }
}
