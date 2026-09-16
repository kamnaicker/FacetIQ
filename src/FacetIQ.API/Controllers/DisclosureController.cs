using System.Security.Claims;
using FacetIQ.API.Mapping;
using FacetIQ.Contracts.Disclosure;
using FacetIQ.Data.Identity;
using FacetIQ.Domain.Abstractions.Repositories;
using FacetIQ.Domain.Abstractions.Services;
using FacetIQ.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FacetIQ.API.Controllers;

[ApiController]
[Route("[controller]")]
public class DisclosureController : ControllerBase
{
    private readonly IDisclosureEvaluator _evaluator;
    private readonly ISubjectRepository _subjects;
    private readonly UserManager<AppUser> _users;

    public DisclosureController(
        IDisclosureEvaluator evaluator,
        ISubjectRepository subjects,
        UserManager<AppUser> users)
    {
        _evaluator = evaluator;
        _subjects = subjects;
        _users = users;
    }

    /// <summary>Evaluates a disclosure request. A refusal is still a 200 with a Deny outcome.</summary>
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

        var subjectId = await SubjectFor(dto.SubjectEmail, cancellationToken);

        if (!DisclosureMapper.TryToDomain(dto, subjectId, requesterUserId, RequestChannel.Api, out var request))
        {
            ModelState.AddModelError(nameof(dto.Purpose), $"Unrecognised purpose '{dto.Purpose}'.");

            // Without an explicit status this is not a 400.
            return ValidationProblem(
                statusCode: StatusCodes.Status400BadRequest,
                modelStateDictionary: ModelState);
        }

        var result = await _evaluator.EvaluateAsync(request, cancellationToken);

        return Ok(DisclosureMapper.ToContract(result));
    }

    // An unknown address resolves to Guid.Empty and is refused as NoMatchingNorm, the same as a
    // subject with no matching rule.
    private async Task<Guid> SubjectFor(string email, CancellationToken cancellationToken)
    {
        var user = await _users.FindByEmailAsync(email);

        if (user is null)
        {
            return Guid.Empty;
        }

        var subject = await _subjects.FindByUserIdAsync(user.Id, cancellationToken);

        return subject?.Id ?? Guid.Empty;
    }
}
