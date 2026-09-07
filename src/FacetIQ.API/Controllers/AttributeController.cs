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

    public AttributeController(ISubjectRepository subjects, IAttributeRepository attributes)
    {
        _subjects = subjects;
        _attributes = attributes;
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
    /// Adds a claim. Nothing is checked against the claims already held: two identical names are
    /// permitted, and so is a contradictory one. The system does not adjudicate between a person's
    /// accounts of themselves.
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

        await _attributes.AddAsync(attribute, cancellationToken);

        return CreatedAtAction(nameof(Get), AttributeMapper.ToContract(attribute));
    }
}
