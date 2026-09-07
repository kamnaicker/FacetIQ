using System.Security.Claims;
using FacetIQ.API.Mapping;
using FacetIQ.Contracts.Norms;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace FacetIQ.API.Controllers;

[ApiController]
[Route("[controller]")]
public class NormController : ControllerBase
{
    private readonly ISubjectRepository _subjects;
    private readonly INormRepository _norms;
    private readonly IConflictDetector _detector;

    public NormController(
        ISubjectRepository subjects,
        INormRepository norms,
        IConflictDetector detector)
    {
        _subjects = subjects;
        _norms = norms;
        _detector = detector;
    }

    /// <summary>
    /// The caller's own norms. There is no route to anyone else's: the subject is resolved from
    /// the token, so the only collection a caller can read is the one they authored.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NormResponse>>> Get(CancellationToken cancellationToken)
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

        var norms = await _norms.ListGoverningAsync(subject.Id, cancellationToken);

        return Ok(norms.Select(NormMapper.ToContract).ToList());
    }

    /// <summary>
    /// Authors a norm. Conflicts are detected before anything is written, so a rule that could not
    /// be told apart from one already in force is refused now rather than stored and discovered
    /// months later, when a request ties and the subject no longer remembers what they wrote.
    ///
    /// The refusal is a 409 rather than a 400: the request is well formed and the subject is
    /// entitled to make it. What it collides with is the state of their own profile.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<NormResponse>> Post(
        CreateNormRequest dto,
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

        if (!NormMapper.TryToDomain(dto, subject.Id, out var norm, out var invalidMember))
        {
            var offending = invalidMember switch
            {
                nameof(dto.Purpose) => dto.Purpose,
                nameof(dto.Transform) => dto.Transform,
                _ => dto.DenyReason
            };

            ModelState.AddModelError(invalidMember!, $"Unrecognised value '{offending}'.");

            return ValidationProblem(
                statusCode: StatusCodes.Status400BadRequest,
                modelStateDictionary: ModelState);
        }

        var existing = await _norms.ListGoverningAsync(subject.Id, cancellationToken);
        var conflicts = _detector.Detect(norm, existing);

        if (conflicts.Count > 0)
        {
            return Conflict(NormMapper.ToContract(conflicts));
        }

        await _norms.AddAsync(norm, cancellationToken);

        return CreatedAtAction(nameof(Get), NormMapper.ToContract(norm));
    }
}
