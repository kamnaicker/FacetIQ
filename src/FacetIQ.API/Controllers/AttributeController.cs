using System.Security.Claims;
using FacetIQ.API.Mapping;
using FacetIQ.Contracts.Attributes;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace FacetIQ.API.Controllers;

[ApiController]
[Route("[controller]")]
public class AttributeController : ControllerBase
{
    private readonly ISubjectRepository _subjects;
    private readonly IAttributeRepository _attributes;
    private readonly INormRepository _norms;
    private readonly IClaimValueValidator _values;

    public AttributeController(
        ISubjectRepository subjects,
        IAttributeRepository attributes,
        INormRepository norms,
        IClaimValueValidator values)
    {
        _subjects = subjects;
        _attributes = attributes;
        _norms = norms;
        _values = values;
    }

    /// <summary>The caller's own claims. Others read claims through /disclosure.</summary>
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

    /// <summary>Adds a claim. Differing values under one key are allowed; an exact duplicate is not.</summary>
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

        if (!_values.IsValid(attribute.Key, attribute.Value, out var problem))
        {
            ModelState.AddModelError(nameof(dto.Value), problem);

            return ValidationProblem(
                statusCode: StatusCodes.Status400BadRequest,
                modelStateDictionary: ModelState);
        }

        // Case-sensitive: a different capitalisation is a different name. The collection purpose
        // counts too: one value held under two ceilings is two claims, and a rule binds to one.
        // The label does not, since it is a note to self and the engine cannot tell two apart by it.
        var held = await _attributes.ListByKeyAsync(subject.Id, attribute.Key, cancellationToken);

        if (held.Any(claim => claim.Value == attribute.Value && claim.CollectedFor == attribute.CollectedFor))
        {
            ModelState.AddModelError(nameof(dto.Value), "You already hold this for that reason.");

            return ValidationProblem(
                statusCode: StatusCodes.Status400BadRequest,
                modelStateDictionary: ModelState);
        }

        await _attributes.AddAsync(attribute, cancellationToken);

        return CreatedAtAction(nameof(Get), AttributeMapper.ToContract(attribute));
    }

    /// <summary>Deletes a claim. Returns 409 listing the rules in force that still select it.</summary>
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
