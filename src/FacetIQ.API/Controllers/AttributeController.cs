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
    /// The caller's own claims. There is no route to anyone else's: a claim is read through the
    /// disclosure endpoint, where a norm decides what a requester receives. This route is the
    /// subject reading their own profile, so it returns the set rather than a selection from it.
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
    /// Adds a claim. The subject comes from the token and never from the route or the body, so a
    /// caller can only ever write into their own profile.
    ///
    /// Nothing is checked against the claims already held. Two identical names are permitted, and
    /// so is a name that contradicts another: the system stores what a person says about
    /// themselves and does not adjudicate between their accounts of it.
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
            return BadRequest($"Unrecognised purpose '{dto.CollectedFor}'.");
        }

        await _attributes.AddAsync(attribute, cancellationToken);

        return CreatedAtAction(nameof(Get), AttributeMapper.ToContract(attribute));
    }
}
