using System.Security.Claims;
using FacetIQ.API.Mapping;
using FacetIQ.Contracts.Attributes;
using FacetIQ.Domain.Abstractions.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FacetIQ.API.Controllers;

[ApiController]
[Route("[controller]")]
public class AttributeController : ControllerBase
{
    private readonly ISubjectRepository _subjects;
    private readonly IAttributeRepository _attributes;
    private readonly INormRepository _norms;

    public AttributeController(
        ISubjectRepository subjects,
        IAttributeRepository attributes,
        INormRepository norms)
    {
        _subjects = subjects;
        _attributes = attributes;
        _norms = norms;
    }

    /// <summary>
    /// The caller's own claims, returned as the whole set. Anyone else reads a claim through the
    /// disclosure endpoint, where a norm decides which one they receive.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AttributeResponse>>> Get(
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

        var claims = await _attributes.ListBySubjectAsync(subject.Id, cancellationToken);

        return Ok(claims.Select(AttributeMapper.ToContract).ToList());
    }

    /// <summary>
    /// Adds a claim. Contradictory claims are allowed, since the system does not adjudicate
    /// between a person's accounts of themselves, but an identical one is refused: a second copy
    /// adds nothing, and two rules releasing the same text through two copies would read as a
    /// conflict between them.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AttributeResponse>> Post(
        CreateAttributeRequest dto,
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

        if (!AttributeMapper.TryToDomain(dto, subject.Id, out var attribute))
        {
            ModelState.AddModelError(
                nameof(dto.CollectedFor),
                $"Unrecognised purpose '{dto.CollectedFor}'.");

            return ValidationProblem(
                statusCode: StatusCodes.Status400BadRequest,
                modelStateDictionary: ModelState);
        }

        // Compared exactly. A different capitalisation or spelling is a different name.
        var held = await _attributes.ListByKeyAsync(subject.Id, attribute.Key, cancellationToken);

        if (held.Any(claim => claim.Value == attribute.Value))
        {
            ModelState.AddModelError(nameof(dto.Value), "You already hold this.");

            return ValidationProblem(
                statusCode: StatusCodes.Status400BadRequest,
                modelStateDictionary: ModelState);
        }

        await _attributes.AddAsync(attribute, cancellationToken);

        return CreatedAtAction(nameof(Get), AttributeMapper.ToContract(attribute));
    }

    /// <summary>
    /// Erases a claim. Refused while a rule in force still releases it, and the refusal names those
    /// rules, so nothing others can see changes behind the subject's back.
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

        var claim = await _attributes.FindAsync(id, cancellationToken);

        if (claim is null || claim.SubjectId != subject.Id)
        {
            return NotFound();
        }

        var releasing = (await _norms.ListGoverningAsync(subject.Id, cancellationToken))
            .Where(norm => norm.AttributeId == id)
            .ToList();

        if (releasing.Count > 0)
        {
            return Conflict(new ClaimInUseResponse
            {
                Rules = releasing.Select(NormMapper.ToContract).ToList(),
            });
        }

        await _attributes.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
