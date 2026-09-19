using System.Security.Claims;
using FacetIQ.API.Mapping;
using FacetIQ.Contracts.Norms;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Abstractions.Services;
using FacetIQ.Domain.Enums;
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
    private readonly ITransformService _transforms;
    private readonly TimeProvider _clock;

    public NormController(
        ISubjectRepository subjects,
        INormRepository norms,
        IAttributeRepository attributes,
        IConflictDetector detector,
        ITransformService transforms,
        TimeProvider clock)
    {
        _subjects = subjects;
        _norms = norms;
        _attributes = attributes;
        _detector = detector;
        _transforms = transforms;
        _clock = clock;
    }

    /// <summary>Retires a rule. It stops governing at once; the row is kept until its claim is deleted.</summary>
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

    /// <summary>The caller's rules in force.</summary>
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

    /// <summary>Authors a rule. Returns 409 with the collisions if it would tie with a rule in force.</summary>
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

            return Invalid(invalidMember!, $"Not an accepted value: '{offending}'.");
        }

        // Only an age threshold reads a parameter; any other would be stored and never used.
        if (norm.TransformParameter is not null && norm.Transform != TransformKind.Generalise)
        {
            return Invalid(nameof(dto.TransformParameter), "Only used when showing an age threshold.");
        }

        // Missing and foreign share one message so neither is confirmed.
        var claim = await _attributes.FindAsync(norm.AttributeId, cancellationToken);

        if (claim is null || claim.SubjectId != subject.Id)
        {
            return Invalid(nameof(dto.AttributeId), "Not one of your claims.");
        }

        if (norm.Action != ActionType.Deny &&
            !_transforms.TryApply(norm.Transform, norm.TransformParameter, claim.Value, out _))
        {
            return Invalid(nameof(dto.Transform), "Cannot be applied to this claim with that parameter.");
        }

        // The collection purpose overrides the rule at evaluation, so it could never release the claim.
        if (norm.Action != ActionType.Deny &&
            claim.CollectedFor is not null &&
            norm.Purpose is not null &&
            norm.Purpose != claim.CollectedFor)
        {
            return Invalid(nameof(dto.Purpose), $"This claim is limited to {claim.CollectedFor} use.");
        }

        // Only norms on the same key can govern the same request.
        var existing = await _norms.GetGoverningNormsAsync(subject.Id, claim.Key, cancellationToken);
        var conflicts = _detector.Detect(norm, existing);

        if (conflicts.Count > 0)
        {
            return Conflict(NormMapper.ToContract(conflicts));
        }

        await _norms.AddAsync(norm, cancellationToken);

        return CreatedAtAction(nameof(Get), NormMapper.ToContract(norm));
    }

    private ActionResult Invalid(string field, string message)
    {
        ModelState.AddModelError(field, message);

        return ValidationProblem(
            statusCode: StatusCodes.Status400BadRequest,
            modelStateDictionary: ModelState);
    }
}
