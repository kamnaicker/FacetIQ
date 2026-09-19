using System.Security.Claims;
using FacetIQ.Contracts.Subjects;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Services.Subjects;
using Microsoft.AspNetCore.Mvc;

namespace FacetIQ.API.Controllers;

[ApiController]
[Route("[controller]")]
public class SubjectController : ControllerBase
{
    private readonly ISubjectRepository _subjects;
    private readonly SubjectProvisioner _provisioner;

    public SubjectController(ISubjectRepository subjects, SubjectProvisioner provisioner)
    {
        _subjects = subjects;
        _provisioner = provisioner;
    }

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

    /// <summary>Returns the caller's profile, creating it if confirmation did not. Safe to call on every sign-in.</summary>
    [HttpPost]
    public async Task<ActionResult<SubjectResponse>> Post(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return Unauthorized();
        }

        var subject = await _provisioner.EnsureAsync(userId, cancellationToken);

        return Ok(new SubjectResponse { Id = subject.Id });
    }
}
