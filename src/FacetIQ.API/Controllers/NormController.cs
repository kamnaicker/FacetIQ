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
    private readonly IAttributeRepository _attributes;
    private readonly IConflictDetector _detector;
    private readonly TimeProvider _clock;

    public NormController(
        ISubjectRepository subjects,
        INormRepository norms,
        IAttributeRepository attributes,
        IConflictDetector detector,
        TimeProvider clock)
    {
        _subjects = subjects;
        _norms = norms;
        _attributes = attributes;
        _detector = detector;
        _clock = clock;
    }

    /// <summary>
    /// Removes a rule by retiring it. It stops governing at once but stays readable, so audit
    /// records that name it still describe what happened.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
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

        var governing = await _norms.ListGoverningAsync(subject.Id, cancellationToken);

        if (governing.All(norm => norm.Id != id))
        {
            return NotFound();
        }

        await _norms.RetireAsync(id, _clock.GetUtcNow(), cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// The caller's own norms. The subject comes from the token, so there is no route to anyone
    /// else's.
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
    /// Authors a norm. Conflicts are detected before anything is written, so a tie is refused now
    /// rather than surfacing against some later request. A collision is a 409, not a 400: the
    /// request is well formed, and what it collides with is the state of the subject's profile.
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

        // The foreign key only proves the claim exists. Without this check a rule could select
        // another subject's claim and the engine would release it. Missing and foreign share one
        // message so the response does not confirm which.
        var claim = await _attributes.FindAsync(norm.AttributeId, cancellationToken);

        if (claim is null || claim.SubjectId != subject.Id)
        {
            ModelState.AddModelError(nameof(dto.AttributeId), "Not one of your claims.");

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
