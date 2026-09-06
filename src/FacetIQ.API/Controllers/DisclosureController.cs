using System.Security.Claims;
using FacetIQ.API.Mapping;
using FacetIQ.Contracts.Disclosure;
using FacetIQ.Domain.Abstractions.Services;
using FacetIQ.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FacetIQ.API.Controllers;

[ApiController]
[Route("[controller]")]
public class DisclosureController : ControllerBase
{
    private readonly IDisclosureEvaluator _evaluator;

    public DisclosureController(IDisclosureEvaluator evaluator) => _evaluator = evaluator;

    /// <summary>
    /// Evaluates one disclosure request. The response varies with the caller and the stated
    /// purpose, so the same route returns different representations of the same claim.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DisclosureResponseDto>> Post(
        DisclosureRequestDto dto,
        CancellationToken cancellationToken)
    {
        var requesterUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (requesterUserId is null)
        {
            return Unauthorized();
        }

        if (!DisclosureMapper.TryToDomain(dto, requesterUserId, RequestChannel.Api, out var request))
        {
            return BadRequest($"Unrecognised purpose '{dto.Purpose}'.");
        }

        var result = await _evaluator.EvaluateAsync(request, cancellationToken);

        // A refusal is a completed evaluation, not a failed request: it carries a reason and
        // an audit record, so it is returned as 200 with an explicit outcome.
        return Ok(DisclosureMapper.ToContract(result));
    }
}
