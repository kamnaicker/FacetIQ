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

        var subjectId = await SubjectFor(dto.SubjectEmail, cancellationToken);

        if (!DisclosureMapper.TryToDomain(dto, subjectId, requesterUserId, RequestChannel.Api, out var request))
        {
            ModelState.AddModelError(nameof(dto.Purpose), $"Unrecognised purpose '{dto.Purpose}'.");

            // Stated explicitly: left unset, the result does not become a 400.
            return ValidationProblem(
                statusCode: StatusCodes.Status400BadRequest,
                modelStateDictionary: ModelState);
        }

        var result = await _evaluator.EvaluateAsync(request, cancellationToken);

        // A refusal is a completed evaluation, not a failed request, so it is a 200 with an outcome.
        return Ok(DisclosureMapper.ToContract(result));
    }

    // An address with no profile resolves to no one rather than to an error. The engine then
    // refuses exactly as it would for a real person with no matching rule, so the response cannot
    // be used to learn whether someone has an account.
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
